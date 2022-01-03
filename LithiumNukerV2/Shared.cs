// Sys
using System;
using System.Text;
using System.Diagnostics;
using System.Dynamic;
using System.Net;
using System.IO;
using System.Drawing;

// Custom
using Veylib;
using Veylib.CLIUI;
using Veylib.Authentication;

// Nuget
using Newtonsoft.Json;

namespace LithiumNukerV2
{
    public class LithiumShared
    {
        private static Core core = Core.GetInstance();

        public static void ExceptionReport(Exception ex, bool auto = true)
        {
            Debug.WriteLine(ex);

            // Don't send error reports in debug mode
            if (Settings.Debug)
                return;

            core.WriteLine("Creating exception report...");
            Debug.WriteLine("Exception report time!");

            var req = WebRequest.Create("https://verlox.cc/api/v2/auth/lithium/reporterror");
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers.Add("Authorization", User.CurrentUser.Token);
            req.Headers.Add("HWID", Shared.HWID);

            dynamic body = new ExpandoObject();
            body.stacktrace = ex.StackTrace;
            body.error = ex.Message;
            body.auto = auto;

            byte[] bodyBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));
            req.GetRequestStream().Write(bodyBytes, 0, bodyBytes.Length);

            try
            {
                dynamic json = JsonConvert.DeserializeObject(new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd());
                if ((int)json.code == 200)
                    core.WriteLine(Color.Lime, "Exception report successfully submitted");
            }
            catch (Exception ex2)
            {
                core.WriteLine(Color.Red, $"Failed to send exception report: {ex2.Message}");
            }
        }
    }
}
