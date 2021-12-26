using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Text.RegularExpressions;

using Veylib.CLIUI;
using LithiumCore;

namespace LithiumNukerV2
{
    internal class Picker
    {
        public static Core core = Core.GetInstance();

        // Components
        private static Channels channels = new Channels();
        private static Webhooks webhooks = new Webhooks();
        private static Bot bot = new Bot();

        public static void Choose()
        {
        EnterToken:

            core.Clear();

            var regex = new Regex(@"[\w-]{24}.[\w-]{6}.[\w-]{27}");
            string token = core.ReadLine("Token : ");

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
                    Settings.Token = token;
            }

        EnterGuildId:

            core.Clear();

            bool success = long.TryParse(core.ReadLine("Guild ID : "), out long gid);
            if (!success)
            {
                core.WriteLine(Color.Red, "Guild ID couldn't be parsed.");
                core.Delay(2500);
                goto EnterGuildId;
            } else
            {
                if (!bot.IsInGuild(gid))
                {
                    core.WriteLine(Color.Red, "Bot is not in guild.");
                    core.Delay(2500);
                    goto EnterGuildId;
                }
            }

            while (true)
            {
                core.Clear();

                var table = new AsciiTable(new AsciiTable.Properties { Colors = new AsciiTable.ColorProperties { RainbowDividers = true } });
                table.AddColumn("1 - Create channels");
                table.AddColumn("2 - Webhook spam channels");
                table.AddColumn("3 - Ban all");

                table.WriteTable();

                var choice = core.ReadLine("Choice : ");

                if (!int.TryParse(choice, out int ch))
                {
                    core.WriteLine("Invalid choice");
                    core.Delay(2500);
                    continue;
                }

                switch (ch)
                {
                    case 1:
                        createChans();
                        break;
                    case 2:
                        whSpam();
                        break;
                    case 3:
                        banAll();
                        break;
                    default:
                        core.WriteLine("Invalid choice");
                        core.Delay(2500);
                        break;
                }
            }
        }

        private static void createChans()
        {
            string name = core.ReadLine("Channel name : ");
            string type = core.ReadLine("Type [text, voice] : ");

            if (type.ToLower() != "text" && type.ToLower() != "voice")
            {
                core.WriteLine(Color.Red, "Invalid channel type");
                core.Delay(2500);
                return;
            }

            if (name == "")
                name = "ran by lithium";

            channels.Spam(name, Channels.Type.Text, 50);

        }

        private static void whSpam()
        {
            string content = core.ReadLine("Content : ");

            if (content == "")
                content = "@everyone discord.gg/lithium";

            webhooks.Spam(content);
        }

        private static void banAll()
        {
            
        }
    }
}
