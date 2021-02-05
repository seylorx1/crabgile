using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using CrabgileDiscordBot.Meeting;
using DSharpPlus.Entities;

namespace CrabgileDiscordBot {
    class CrabgileCommandModule : BaseCommandModule {

        [Command("timebox")]
        public async Task TimeboxCommand(CommandContext ctx, int time) {
            await ctx.RespondAsync($"Creating a {time} minute timebox...\n**Type !begin to start the timebox session, {ctx.Member.Mention}.**\n*(Alternatively, you can type !end to end the meeting prematurely.)*");

            if (!CrabgileMeeting.IsDiscordUserMeetingHost(ctx.Member)) {
                new CrabgileTimebox(ctx.Member, time);
            }
            else {
                await ctx.RespondAsync($"You are already hosting a meeting, {ctx.Member.Mention}!");
            }

            await TryDeleteUserMessage(ctx);
        }

        [Command("timebox"), Aliases("scrum")]
        public async Task TimeboxCommand(CommandContext ctx) {
            await TimeboxCommand(ctx, 15);
        }

        [Command("meeting")]
        public async Task MeetingCommand(CommandContext ctx) {
            await ctx.RespondAsync($"Creating a meeting...\n**Type !end to close the meeting, {ctx.Member.Mention}.**");

            if (!CrabgileMeeting.IsDiscordUserMeetingHost(ctx.Member)) {
                CrabgileMeeting meeting = new CrabgileMeeting(ctx.Member);
                meeting.AddChannelToMeeting(await ctx.Guild.CreateVoiceChannelAsync("Meeting Voice", await meeting.GetMeetingCategory()));
            }
            else {
                await ctx.RespondAsync($"You are already hosting a meeting, {ctx.Member.Mention}!");
            }
            await TryDeleteUserMessage(ctx);
        }

        [Command("end"), Aliases("end-meeting")]
        public async Task EndMeetingCommand(CommandContext ctx) {
            if(CrabgileMeeting.IsDiscordUserMeetingHost(ctx.Member)) {
                await CrabgileMeeting.GetMeetingFromHost(ctx.Member).CloseMeeting();
            }
            else {
                await ctx.RespondAsync($"I'm afraid there's no meeting in progress, {ctx.Member.Mention}!");
            }
            await TryDeleteUserMessage(ctx);
        }

        [Command ("begin"), Aliases("begin-meeting")]
        public async Task StartMeetingCommand(CommandContext ctx) {
            CrabgileMeeting crabgileMeeting = CrabgileMeeting.GetMeetingFromHost(ctx.Member);
            if (crabgileMeeting is CrabgileTimebox) {
                ((CrabgileTimebox)crabgileMeeting).BeginTimeboxMeeting();
            }
            await TryDeleteUserMessage(ctx);
        }

        public async Task TryDeleteUserMessage(CommandContext ctx) {
            //Delete user message.
            try {
                await ctx.Message.DeleteAsync(CrabgileConstants.DELETE_USER_MESSAGE);
                return;
            }
            catch (Exception) {
                Console.WriteLine(CrabgileConstants.INVALID_PERM_MESSAGE);
            }
            await Task.CompletedTask;
            return;
        }
    }
}
