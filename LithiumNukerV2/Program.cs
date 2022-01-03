// System
using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.IO;
using System.Text;
using Microsoft.Win32;

// Custom
using Veylib;
using Veylib.CLIUI;
using Veylib.Authentication;

// Nuget
using Newtonsoft.Json;
using System.Reflection;

// If you somehow cracked this code, good job, there is minimal effort.
// Only reason there is any obf, is to prevent annoying ass skids.
// Anyways, carry on.
// - verlox
// ps. eat shit

namespace LithiumNukerV2
{
    internal class Program
    {
        // Setup CLIUI
        public static Core core = Core.GetInstance();

        // Parse entry point args
        private static void parseArgs(string[] args)
        {
            for (var x = 0;x < args.Length;x++)
            {
                switch (args[x].ToLower())
                {
                    // Put this shit in debug
                    case "--debug":
                        Settings.Debug = true;
                        break;
                    // Set token on start
                    case "--token":
                        x++;
                        Settings.Token = args[x];
                        break;
                    // Set guild id
                    case "--guild":
                        x++;
                        var succ = long.TryParse(args[x], out long lid);
                        if (!succ)
                            core.WriteLine(Color.Red, "--guild argument value invalid");
                        Settings.GuildId = lid;
                        break;
                    // Means that there was an unparsed arg that is unknown
                    default:
                        core.WriteLine(Color.Red, $"Invalid argument: {args[x].ToLower()}");
                        break;
                }
            }
        }

        private static User.UserData login(string username, string password, string token = null)
        {
            try
            {
                // Login
                User.UserData user;
                if (token != null)
                    user = User.Verify(token);
                else
                    user = User.Verify(username, password);

                // Check user state
                switch (user.State)
                {
                    case User.UserVerificationState.ValidCredentials:
                        // Dll injection
                        if (!File.Exists("LithiumCore.dll"))
                        {
                            core.WriteLine(user.Token);

                            // Web headers
                            var client = new WebClient();
                            client.Headers.Add("Authorization", user.Token);
                            client.Headers.Add("HWID", Shared.HWID);

                            // Download this dumb shit
                            client.DownloadFile("https://verlox.cc/api/v2/auth/lithium/download", "LithiumCore.dll");
                        }
                        else
                            Debug.WriteLine("LithiumCore.dll already downloaded, skipping.");

                        // Save the token to the registry
                        var key = Registry.CurrentUser.CreateSubKey(Settings.RegPath);

                        // Set and close
                        key.SetValue("AccountToken", user.Token);
                        key.Close();
                        
                        Picker.Choose(); // Open options
                        break;
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

                return user;
            }
            catch (WebException ex)
            {
                core.WriteLine(Color.Red, new StreamReader(ex.Response.GetResponseStream()).ReadToEnd());
            }
            catch (Exception ex) // Whoop de doo, another shitty error to deal with at some point
            {
                Debug.WriteLine(ex);
                core.WriteLine(Color.Red, $"Error logging in: {ex.Message}");
            }

            return null;
        }

        // Entry point
        static void Main(string[] args)
        {
            // Setting up auth
            Shared.AppID = 2;
            Shared.APIUrl = "https://verlox.cc/api/v2";

            // No.
            #if DEBUG
            Settings.Debug = true;
            #endif

            // Parse the args
            parseArgs(args);

            #region Setting up the UI
            string motd = Settings.Debug ? "IN DEV BUILD" : "suck a fat cock";
            core.Start(new StartupProperties { MOTD = motd, ColorRotation = 260,  SilentStart = true, LogoString = Settings.Logo, DebugMode = Settings.Debug, Author = new StartupAuthorProperties { Url = "verlox.cc & russianheavy.xyz", Name = "verlox & russian heavy" }, Title = new StartupConsoleTitleProperties { Text = "Lithium Nuker V2", Status = "Authorization required" } });
            #endregion

            // On exit, delete the LithiumCore.dll if you can
            AppDomain.CurrentDomain.ProcessExit += (eat, shit) =>
            {
                File.Delete("LithiumCore.dll");
            };

            // Setup the stupid ass connection limits
            ServicePointManager.DefaultConnectionLimit = Settings.ConnectionLimit; // 20 Similtanious connections
            ServicePointManager.Expect100Continue = false;

            #region Authorization

            // Check for login in registry
            var key = Registry.CurrentUser.OpenSubKey(Settings.RegPath);
            if (key != null)
            {
                string token = (string)key.GetValue("AccountToken");
                key.Close();

                if (token != null)
                    if (login(null, null, token).State == User.UserVerificationState.ValidCredentials)
                        return;
            }

            // Do a while loop so that they have to login
            while (true)
            {
                // Get creds
                string username = core.ReadLine("Username : ");
                string password = core.ReadLineProtected("Password : ");

                login(username, password);
            }

            #endregion
        }
    }
}
