using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using DSharpPlus.Entities;

namespace CrabgileDiscordBot.Meeting {
    class CrabgileTimebox : CrabgileMeeting {
        private int currentTimeboxTime;
        public CrabgileTimebox(DiscordMember hostMember, int time) : base(hostMember) {
            currentTimeboxTime = time * 60;

            Task.Run(async () => {
                //Add voice channel.
                DiscordChannel meetingCategory = await GetMeetingCategory();
                AddChannelToMeeting(await guild.CreateVoiceChannelAsync("Timebox Voice", meetingCategory));
            });
        }

        public async Task<DiscordChannel> CreateTimeboxStatsChannel() {
            //Create timebox voice channel.
            DiscordChannel timeboxStatsChannel = await guild.CreateChannelAsync(
                "timebox-stats",
                DSharpPlus.ChannelType.Text,
                await GetMeetingCategory(),
                "Stats for the timeboxed slot.",
                null,
                null);
            AddChannelToMeeting(timeboxStatsChannel);
            return timeboxStatsChannel;
        }

        public void BeginTimeboxMeeting() {
            Task.Run(async () => {

                DiscordChannel statsChannel = await CreateTimeboxStatsChannel();
                DiscordMessage statsMessage = null;

                while (currentTimeboxTime > 0 && !meetingClosed) {

                    //Refresh the channel.
                    statsChannel = guild.GetChannel(statsChannel.Id);

                    //Make sure stats channel exists.
                    if (statsChannel == null) {
                        statsChannel = await CreateTimeboxStatsChannel();
                    }


                    //Handle message deletions.
                    if (statsMessage != null) {
                        try {
                            statsMessage = await statsChannel.GetMessageAsync(statsMessage.Id);
                        }
                        catch (Exception) {
                            await statsChannel.AddOverwriteAsync(guild.EveryoneRole, DSharpPlus.Permissions.SendMessages, DSharpPlus.Permissions.None);
                            statsMessage = null;
                        }
                    }

                    if (statsMessage == null) {
                        try {
                            statsMessage = await new DiscordMessageBuilder()
                                .WithContent(getTimeboxStatsMessage())
                                .SendAsync(statsChannel);
                        }
                        catch(Exception) {
                            //Cry me a fucking river.
                        }
                        await statsChannel.AddOverwriteAsync(guild.EveryoneRole, DSharpPlus.Permissions.None, DSharpPlus.Permissions.SendMessages);
                    }
                    else {
                        try {
                            await statsMessage.ModifyAsync(getTimeboxStatsMessage());
                        }
                        catch (Exception) {
                            //Again, just throwing random ass exceptions for no apparent reason.
                            //Thanks DSharpPlus, you're my favourite API :D
                        }
                    }

                    //10 second update.
                    await Task.Delay(1000 * 10);
                    currentTimeboxTime-=10;
                }

                await CloseMeeting();
            });
        }

        private string getTimeboxStatsMessage() {
            int minutes = currentTimeboxTime / 60;
            int seconds = currentTimeboxTime % 60;

            return
                $"**Timebox Meeting Stats**\n" +
                $"*{minutes} minute{(minutes != 1 ? "s" : "")} and {seconds} seconds left.*\n\n" +
                $"*(Updates every 10 seconds. This is to prevent the bot from being rate-limited by Discord, and cannot be changed.)*";
        }
    }
}
