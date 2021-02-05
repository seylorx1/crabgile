using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;

using CrabgileDiscordBot.Meeting;

//https://discord.com/api/oauth2/authorize?client_id=806864336655220746&permissions=469953650&scope=bot

namespace CrabgileDiscordBot {
    class CrabgileDiscordBot {
        static void Main(string[] args) {
            Console.WriteLine("Please enter the Discord bot token:");
            string token = Console.ReadLine();
            Console.Clear();

            try {
                string censoredToken = token.Substring(0, 4);
                for (int i = 0; i < token.Length - 8; i++) {
                    censoredToken += ".";
                }
                censoredToken += token.Substring(token.Length - 4, 4);
                Console.WriteLine("Crabgile Discord Bot\nToken: " + censoredToken);
                Task.Run(async () => {
                    while (true) {
                        string input = Console.ReadLine();

                        if (input == "exit" || input == "q") {
                            Console.WriteLine("Closing all active meetings...");
                            CrabgileMeeting.CloseAllMeetings();
                            await CrabgileMeeting.WaitUntilMeetingsClosed();

                            Environment.Exit(1);
                            return;
                        }
                    }
                });
            }
            catch (Exception) {
                //String probably wasn't long enough.
                return;
            }

            MainDiscordBotAsync(token).GetAwaiter().GetResult();
        }

        static async Task MainDiscordBotAsync(string token) {
            var discord = new DiscordClient(new DiscordConfiguration() {
                Token = token,
                TokenType = TokenType.Bot
            });
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration() {
                StringPrefixes = new[] { "!" }
            });

            commands.RegisterCommands<CrabgileCommandModule>();

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
