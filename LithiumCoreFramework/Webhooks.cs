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
                channels = new Channels().GetAll();

            foreach (var chan in channels)
                foreach (var whsChan in chan.GetWebhooks())
                    whs.Add(whsChan);

            return whs;
        }


        public Webhook Create(long channelId)
        {
            // Basic req shit
            var req = WebRequest.Create($"https://discord.com/api/v9/channels/{channelId}/webhooks");
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers.Add("Authorization", $"Bot {Settings.Token}");

            // Create json as a dynamic
            dynamic jsonBody = new ExpandoObject();
            jsonBody.name = Settings.WebhookName;

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
                core.WriteLine(Color.Green, $"Created webhook {Settings.WebhookName} in {channelId}");
                return wh;
            } catch (Exception ex)
            {
                core.WriteLine(Color.Red, $"Failed to create webhook: {ex.Message}");
            }

            return null;
        }

        public void SendLoop(List<Webhook> webhooks)
        {
            while (true)
            {
                foreach (var wh in webhooks)
                {
                    Send(wh.Url);
                }
            }
        }

        public void Send(string whUrl)
        {
            var req = WebRequest.Create(whUrl);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers.Add("Authorization", $"Bot {Settings.Token}");
            req.Proxy = null;

            dynamic jsonBody = new ExpandoObject();
            jsonBody.username = Name;
            jsonBody.content = Content;
            jsonBody.avatar_url = Settings.AvatarUrl;

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

        public void Spam(string content)
        {
            var channels = new Channels().GetAll();
            var webhooks = new List<Webhook>();

            Content = content;

            var preWhs = GetAll();

            foreach (var prewh in preWhs)
                if (prewh.Name == Settings.WebhookName)
                    webhooks.Add(prewh);

            // Create a webhook for each channel
            
            foreach (Channels.Channel chan in channels)
            {
                if (chan.Type == Channels.Type.Text && webhooks.FindAll(wh => wh.ChannelId == chan.Id).Count == 0)
                {
                    var wh = Create(chan.Id);
                    if (wh != null)
                        webhooks.Add(wh);
                }
            }

            // Create work loads
            var loads = new WorkController().Seperate(webhooks.Cast<dynamic>().ToList());

            foreach (var load in loads)
                new Thread(() => { SendLoop(load.Cast<Webhook>().ToList()); }).Start();
        }
    }
}