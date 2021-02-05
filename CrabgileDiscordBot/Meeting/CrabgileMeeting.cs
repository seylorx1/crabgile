using DSharpPlus.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CrabgileDiscordBot.Meeting {
    class CrabgileMeeting {
        #region Registry
        private static ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, CrabgileMeeting>> GuildMeetings = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, CrabgileMeeting>>();
        
        public static bool IsDiscordUserMeetingHost(DiscordMember member) {

            if(GuildMeetings.ContainsKey(member.Guild.Id)) {
                return GuildMeetings[member.Guild.Id].ContainsKey(member.Id);
            }
            return false;
        }

        public static CrabgileMeeting GetMeetingFromHost(DiscordMember hostMember) {
            if(IsDiscordUserMeetingHost(hostMember)) {
                return GuildMeetings[hostMember.Guild.Id][hostMember.Id];
            }
            return null;
        }

        //TODO This code is a little dodgy, given the guildMeetingDicts are removed on async in CloseMeeting if a guild is empty.
        public static void CloseAllMeetings() {
            foreach(ConcurrentDictionary<ulong, CrabgileMeeting> guildMeetingDict in GuildMeetings.Values) {
                if (guildMeetingDict != null) {
                    foreach (CrabgileMeeting meeting in guildMeetingDict.Values) {
                        Task.Run(() => meeting.CloseMeeting());
                    }
                }
            }
        }

        public static async Task WaitUntilMeetingsClosed() {
            while (!GuildMeetings.IsEmpty) {
                await Task.Delay(10);
            }
            await Task.CompletedTask;
        }
        #endregion

        #region Archive Channel Registry
        private static ConcurrentDictionary<ulong, DiscordChannel> ArchiveChannelCategories;
        public static async Task<DiscordChannel> GetArchiveChannel(DiscordGuild guild) {

            //Create a new registry, if one doesn't already exist.
            if (ArchiveChannelCategories == null) {
                ArchiveChannelCategories = new ConcurrentDictionary<ulong, DiscordChannel>();
            }

            //Check to see if the registry does not contain the guild.
            if (!ArchiveChannelCategories.ContainsKey(guild.Id)) {
                IReadOnlyList<DiscordChannel> allDiscordChannels = await guild.GetChannelsAsync();
                foreach (DiscordChannel channel in allDiscordChannels) {
                    if (channel.Name == CrabgileConstants.CHANNEL_NAME_ARCHIVE) {
                        //Channel exists in server, so add it to the registry and return it.
                        ArchiveChannelCategories.TryAdd(guild.Id, channel);
                        return channel;
                    }
                }

                //No channel at all, so it must be created and added to the registry.
                ArchiveChannelCategories.TryAdd(guild.Id, await guild.CreateChannelCategoryAsync(CrabgileConstants.CHANNEL_NAME_ARCHIVE));
            }

            //Return registry channel.
            return ArchiveChannelCategories[guild.Id];
        }
        #endregion

        protected DiscordMember hostMember;
        protected DiscordGuild guild;
        private ConcurrentBag<DiscordChannel> channels;

        private bool registrySuccess = false;
        protected bool meetingClosed = false;

        private DiscordChannel meetingCategory;
        private DiscordChannel meetingTextChannel;
        public CrabgileMeeting(DiscordMember hostMember) {
            this.hostMember = hostMember;
            guild = hostMember.Guild;
            channels = new ConcurrentBag<DiscordChannel>();

            if (!GuildMeetings.ContainsKey(guild.Id)) {
                GuildMeetings.TryAdd(guild.Id, new ConcurrentDictionary<ulong, CrabgileMeeting>());
            }
            registrySuccess = GuildMeetings[guild.Id].TryAdd(hostMember.Id, this);
            
            if(registrySuccess) {
                Task.Run(async () => {
                    meetingCategory = await guild.CreateChannelCategoryAsync($"::{hostMember.Id}::");
                    AddChannelToMeeting(meetingCategory);
                    meetingTextChannel = await guild.CreateChannelAsync("meeting-chat", DSharpPlus.ChannelType.Text, meetingCategory, "A minuted text channel for the timebox meeting.");
                    //AddChannelToMeeting(meetingTextChannel);
                    //Don't add the channel to the bag, as it shouldn't get destroyed.
                }); 
            }
        }

        public async Task<DiscordChannel> GetMeetingCategory() {
            while(meetingCategory == null) {
                await Task.Delay(10);
            }
            return meetingCategory;
        }
        public async Task<DiscordChannel> GetMeetingTextChannel() {
            while (meetingTextChannel == null) {
                await Task.Delay(10);
            }
            return meetingTextChannel;
        }
        public void AddChannelToMeeting(DiscordChannel channel) {
            channels.Add(channel);
        }

        public virtual async Task CloseMeeting() {

            //Move text meeting to archive.
            DiscordChannel archiveChannel = await GetArchiveChannel(guild);
            DateTimeOffset dateTimeOffset = meetingTextChannel.CreationTimestamp;

            //Check to see if meeting text channel still exists.
            if (guild.GetChannel(meetingTextChannel.Id) != null) {

                //Change the name to 'meeting-hhmm-dd-mm'
                await meetingTextChannel.ModifyAsync(async x => {
                    x.Name = $"meeting-{dateTimeOffset.Hour.ToString("D2")}{dateTimeOffset.Minute.ToString("D2")}-{dateTimeOffset.Day}-{dateTimeOffset.Month}";
                    x.Parent = archiveChannel;
                    x.Topic = "Archived meeting.";
                });

                //Create a archived meeting message.
                await new DiscordMessageBuilder()
                .WithContent(
                    $"**End of archived messages.** (￣o￣) . z Z\n" +
                    $"*Meeting created on **{dateTimeOffset.Day}-{dateTimeOffset.Month}-{dateTimeOffset.Year}** at **{dateTimeOffset.Hour}:{dateTimeOffset.Minute}:{dateTimeOffset.Second.ToString("D2")}**.\n\n" +
                    $"This is an archived channel and can no longer be messaged by users with the 'everyone' permission.\n" +
                    $"Please contact a server administrator for further assistance.* :crab:")
                .SendAsync(meetingTextChannel);

                //Send the message to the channel.
                await meetingTextChannel.AddOverwriteAsync(guild.EveryoneRole, DSharpPlus.Permissions.None, DSharpPlus.Permissions.SendMessages);
            }

            //Delete channels.
            foreach (DiscordChannel channel in channels) {
                //Check channel still exists.
                if (guild.GetChannel(channel.Id) != null) {
                    await channel.DeleteAsync();
                }
            }

            //Remove meeting from registry.
            GuildMeetings[guild.Id].Remove(hostMember.Id, out _);

            //Remove the guild from the list completely if it's empty.
            if(GuildMeetings[guild.Id].IsEmpty) {
                GuildMeetings.Remove(guild.Id, out _);
            }

            meetingClosed = true;
        }

        /// <summary>
        /// Checks to see if the CrabgileMeeting was correctly added to the registry.
        /// </summary>
        /// <returns>A meeting by that host already existed!</returns>
        public bool IsValid() {
            return registrySuccess;
        }
    }
}
