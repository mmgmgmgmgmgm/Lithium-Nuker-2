// System
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;
using System.Drawing;

// Custom
using LithiumNukerV2;
using Veylib.CLIUI;

// Nuget
using Newtonsoft.Json;
using System.Diagnostics;

namespace LithiumCore
{
    public class Roles
    {
        public delegate void noret();
        public static event noret Finished;

        private static Core core = Core.GetInstance();

        private string token;
        private long guildId;
        private int threads;
        private static readonly int timeout = 5 * 1000; // 5 seconds

        public Roles(string tok, long gid, int threadCount)
        {
            token = tok;
            guildId = gid;
            threads = threadCount;
        }

        public class Role
        {
            public Role() { }
            public Role(dynamic raw)
            {
                if (raw.message != null && raw.message == "You are being rate limited")
                    throw new Exception("Ratelimited");

                _raw = raw;
                Id = raw.id;
                Name = raw.name;
            }

            public long Id;
            public string Name;
            public dynamic _raw;

            public void Delete(long guildId, string token)
            {
                int tries = 0;

            Retry:
                tries++;
                if (tries >= 3)
                    throw new Exception("Exceeded max retry limit on creating roles");

                var req = WebRequest.Create($"https://discord.com/api/v9/guilds/{guildId}/roles/{Id}");
                req.Method = "DELETE";
                req.Headers.Add("Authorization", $"Bot {token}");
                req.Headers.Add("X-Audit-Log-Reason", "lithium runs you");
                req.Timeout = timeout;
                WebResponse res = null;
                try
                {
                    res = req.GetResponse();
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.Timeout)
                        goto Retry;

                    dynamic json = JsonConvert.DeserializeObject(new StreamReader(ex.Response.GetResponseStream()).ReadToEnd());

                    if (((string)json.message).Contains("rate limited"))
                    {
                        Thread.Sleep((int)json.retry_after * 1000);
                        goto Retry;
                    }
                    else
                        throw ex;
                }

                if (res != null)
                    res.Dispose();
                return;
            }
        }

        public List<Role> GetAll()
        {
            // Create the return list
            var roles = new List<Role>();

            // Create the request
            var req = WebRequest.Create($"https://discord.com/api/v9/guilds/{guildId}/roles");
            req.Headers.Add("Authorization", $"Bot {token}");
            req.Proxy = null;

            // Setup return vars
            string raw = null;
            dynamic json = null;

            // Get the raw response
            try
            {
                raw = new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
            }
            catch (WebException ex)
            {
                raw = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }

            // Parse and iterate through
            json = JsonConvert.DeserializeObject(raw);
            foreach (var chan in json)
            {
                try
                {
                    roles.Add(new Role(chan));
                }
                catch { }
            }

            // Return the channels collected
            return roles;
        }

        public Role Create(string name, Color color)
        {
            int tries = 0;
        Retry:
            tries++;
            if (tries >= 3)
                throw new Exception("Exceeded max retry limit on creating channel");

            var req = WebRequest.Create($"https://discord.com/api/v9/guilds/{guildId}/roles");
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers.Add("Authorization", $"Bot {token}");
            req.Proxy = null;
            req.Timeout = timeout;

            byte[] body = Encoding.UTF8.GetBytes("{ \"name\": \"" + name + "\", \"color\": " + (65536 * color.R + 256 * color.G + color.B) + " }");
            var reqstr = req.GetRequestStream();
            reqstr.Write(body, 0, body.Length);
            reqstr.Dispose();

            string raw;
            dynamic json;
            Exception error = null;

            WebResponse res = null;
            try
            {
                res = req.GetResponse();
                raw = new StreamReader(res.GetResponseStream()).ReadToEnd();
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                    goto Retry;

                raw = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                error = ex;
            }
            if (res != null)
                res.Close();

            // Conversion and finising
            json = JsonConvert.DeserializeObject(raw);

            if (error != null)
            {
                if (((string)json.message).Contains("rate limited"))
                {
                    Thread.Sleep((int)json.retry_after * 1000);
                    goto Retry;
                }
                else
                    throw error;
            }

            try
            {
                var role = new Role(json);
                return role;
            }
            catch
            {
                return null;
            }
        }

        public List<Role> Spam(string name, int count, Color color)
        {
            var roles = new List<Role>();

            for (var x = 0; x < count; x++)
                roles.Add(null);

            var loads = WorkController.Seperate(roles, threads);

            int created = 0;
            int finished = 0;
            foreach (var load in loads)
            {
                var t = new Thread(() =>
                {
                    for (var x = 0; x < load.Count; x++)
                    {
                        try
                        {
                            var role = Create(name, color);
                            created++;

                            Debug.WriteLine(JsonConvert.SerializeObject(role));
                            core.WriteLine("Created ", Color.White, name, null, " [", Color.White, role.Id.ToString(), null, "]");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            core.WriteLine(Color.Red, $"Failed to create role ", Color.White, name, null, ": ", Color.White, ex.Message);
                        }
                    }
                    lock (finished.GetType())
                        finished++;
                });
                t.Start();
            }

            while (finished < loads.Count)
                Thread.Sleep(50);

            Debug.WriteLine("Finished creating roles");

            core.WriteLine(Color.Lime, $"Created {created} roles");
            Finished?.Invoke();
            return roles;
        }

        public void Nuke()
        {
            // Setup work loads
            var roles = GetAll();
            var loads = WorkController.Seperate(roles, threads);
            var errors = new List<Exception>();
            int finished = 0;

            foreach (var load in loads)
            {
                // Create new thread
                var t = new Thread(() =>
                {
                    // Iterate thru sublist and delete each channel within
                    foreach (var role in load)
                    {
                        if (role.Name == "@everyone")
                            continue;

                        try
                        {
                            role.Delete(guildId, token);
                            core.WriteLine($"Deleted ", Color.White, $"#{role.Name}", null, " [", Color.White, role.Id.ToString(), null, "]");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            core.WriteLine(Color.Red, $"Failed to delete roles {role.Name} [{role.Id}]: {ex.Message}");
                        }
                    }
                    lock (finished.GetType())
                        finished++;
                });
                t.Start();
                // t.Join();
            }

            while (finished < loads.Count)
                Thread.Sleep(50);

            Debug.WriteLine("Finished deleting roles");

            core.WriteLine(Color.Lime, $"Finished nuking {roles.Count - errors.Count} roles");
            Finished?.Invoke();
        }
    }
}
