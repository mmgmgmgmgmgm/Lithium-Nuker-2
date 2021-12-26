using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Veylib.CLIUI;
using LithiumCore;

namespace LithiumNukerV2
{
    internal class Picker
    {
        public static Core core = Core.GetInstance();

        // Components
        public static Channels channels = new Channels();
        public static Webhooks webhooks = new Webhooks();

        public static void Choose()
        {
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
