using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Dynamic;
using System.Text;
using System.Linq;
using System.Threading;

using LithiumNukerV2;
using Newtonsoft.Json;

namespace LithiumCore
{
    public class Channels
    {
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
            Stage
        }

        public class Channel
        {
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

            dynamic jsonBody = new ExpandoObject();
            jsonBody.name = name;
            jsonBody.topic = "discord.gg/lithium | Lithium runs all.";
            
            switch (type)
            {
                case Type.Text:
                    jsonBody.type = 0;
                    break;
                case Type.Voice:
                    jsonBody.type = 2;
                    break;
                case Type.Stage:
                    jsonBody.type = 13;
                    break;
            }

            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonBody));
            req.GetRequestStream().Write(body, 0, body.Length);

            string raw;
            dynamic json;

            try 
            {
                raw = new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
            } catch (WebException ex)
            {
                raw = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
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

            for (var x = 0; x < (count < threads ? count : threads); x++)
            {
                new Thread(() =>
                {
                    for (var y = 0; (y - 1) < (count / (count < threads ? count : threads)); y++)
                    {
                        System.Diagnostics.Debug.WriteLine($"{(count / (count < threads ? count : threads))} Revs");
                        channels.Add(Create(name, type));
                    }
                }).Start();
            }

            return channels;
        }
    }
}
