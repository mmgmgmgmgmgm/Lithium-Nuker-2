// Sys shit
using System;
using System.Net;

// Custom
using LithiumNukerV2;

// Nuget
using Newtonsoft.Json;

namespace LithiumCore
{
    public class Bot
    {
        public bool TestToken(string token)
        {
            var req = WebRequest.Create("https://discord.com/api/v9/users/@me");
            req.Headers.Add("Authorization", $"Bot {token}");

            try
            {
                req.GetResponse();
                return true;
            }
            catch { return false; }
        }

        public bool IsInGuild(long guildId)
        {
            var req = WebRequest.Create($"https://discord.com/api/v9/guilds/{guildId}");
            req.Headers.Add("Authorization", $"Bot {Settings.Token}");

            try
            {
                req.GetResponse();
                return true;
            }
            catch { return false; }
        }
    }
}
