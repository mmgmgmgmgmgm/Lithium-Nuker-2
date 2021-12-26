// System
using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Reflection;

// Nonsystem / custom
using Veylib;
using Veylib.CLIUI;
using Veylib.Authentication;

namespace LithiumNukerV2
{
    internal class Program
    {
        public static Core core = Core.GetInstance();

        private static void parseArgs(string[] args)
        {
            for (var x = 0;x < args.Length;x++)
            {
                switch (args[x].ToLower())
                {
                    case "--debug":
                        Settings.Debug = true;
                        break;
                    case "--token":
                        x++;
                        Settings.Token = args[x];
                        break;
                    case "--guild":
                        x++;
                        var succ = long.TryParse(args[x], out long lid);
                        if (!succ)
                            core.WriteLine(Color.Red, "--guild argument value invalid");
                        Settings.GuildId = lid;
                        break;
                    default:
                        core.WriteLine(Color.Red, $"Invalid argument: {args[x].ToLower()}");
                        break;
                }
            }
        }

        static void Main(string[] args)
        {
            Shared.AppID = 2;
            Shared.APIUrl = "https://verlox.cc/api/v2";

            #if DEBUG
            Settings.Debug = true;
            #endif

            #region Parse args

            #endregion

            #region Setting up the UI

            string motd = Settings.Debug ? "IN DEV BUILD" : "suck a fat cock";
            core.Start(new StartupProperties { MOTD = motd, ColorRotation = 260,  SilentStart = true, LogoString = Settings.Logo, Author = new StartupAuthorProperties { Url = "verlox.cc & russianheavy.xyz", Name = "verlox & russian heavy" }, Title = new StartupConsoleTitleProperties { Text = "Lithium Nuker V2", Status = "Authorization required" } });

            #endregion

            // ************************************************************ //

            //string token = core.ReadLine("Token : ");
            //int connectionLimit = int.Parse(core.ReadLine("Connection limit : "));
            //long guildId = long.Parse(core.ReadLine("Guild ID : "));

            //int opt = int.Parse(core.ReadLine("1 - channels, 2 - webhook : "));

            ServicePointManager.DefaultConnectionLimit = Settings.ConnectionLimit; // 20 Similtanious connections
            ServicePointManager.Expect100Continue = false;

            //// Testing webhook spamming only, skip auth for now
            //Settings.Token = token;
            //Settings.GuildId = guildId;
            //Settings.WebhookName = ".gg/lithium runs you";

            //if (opt == 1)
            //else
            //    
            //return;

            

            // ************************************************************ //

            #region Authorization

            // Do a while loop so that they have to login
            while (true)
            {
                // Get creds
                string username = core.ReadLine("Username : ");
                string password = core.ReadLineProtected("Password : ");

                try
                {
                    // Login
                    var user = User.Verify(username, password);

                    // Check user state
                    switch (user.State)
                    {
                        case User.UserVerificationState.ValidCredentials:
                              Picker.Choose(); // Open options
                            return;
                        case User.UserVerificationState.AccountDisabled:
                            core.WriteLine(Color.Red, "Account is disabled.");
                            break;
                        case User.UserVerificationState.ApplicationDisabled:
                            core.WriteLine(Color.Red, "Application is disabled.");
                            break;
                        case User.UserVerificationState.InvalidHWID:
                            core.WriteLine(Color.Red, "HWID invalid.");
                            break;
                        case User.UserVerificationState.InvalidCredentials:
                            core.WriteLine(Color.Red, "Invalid credentials");
                            break;
                    }
                } catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    core.WriteLine(Color.Red, $"Error logging in: {ex.Message}");
                }
            }

            #endregion
        }
    }
}
