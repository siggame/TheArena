using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TheArena
{
    public class MyPacket
    {
        public string status;
        public string winReason;
        public string loseReason;
        public string logUrl;
    }

    public class winner
    {
        public string teamName;
        public string version;
    }

    public class loser
    {
        public string teamName;
        public string version;
    }

    public class HTTP
    {
        public static void HTTPPost(string status, string winReason, string loseReason, string logURL, string winnerTeamName, string winnerVersion, string loserTeamName, string loserVersion)
        {
            /*winReason=winReason.Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "");
            winReason = winReason.Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "");
            winReason = winReason.Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "");
            loseReason =loseReason.Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "");
            loseReason = loseReason.Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "");
            loseReason = loseReason.Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "");
            status =status.Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "");
            status = status.Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "");
            status = status.Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "");
            logURL =logURL.Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "").Replace("\"", "");
            logURL = logURL.Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "").Replace("\n", "");
            logURL = logURL.Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "").Replace("\r", "");
            string myJson =
                "{" +
                "\"status\": \"" + status + "\"," +
                "\"winReason\": \"" + winReason + "\"," +
                "\"loseReason\": \"" + loseReason + "\"," +
                "\"logUrl\": \"" + logURL + "\"," +
                "\"winner\": {" +
                    "\"teamName\":\"" + winnerTeamName + "\"," +
                    "\"version\":\"" + winnerVersion + "\"" +
                "}," +
                "\"loser\": {" +
                    "\"teamName\":\"" + loserTeamName + "\"," +
                    "\"version\":\"" + loserVersion + "\"" +
                "}" +
                "}";*/
            MyPacket p = new MyPacket() { status = status, loseReason = loseReason, winReason = winReason, logUrl = logURL };
            string serialized = JsonConvert.SerializeObject(p);
            Console.WriteLine(serialized);
            using (var client = new HttpClient())
            {
                try
                {
                    List<Task> allGames = new List<Task>();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VybmFtZSI6IklMaWtlU29ja3NPblN1bmRheXMiLCJpZCI6Miwicm9sZSI6InVzZXIiLCJpYXQiOjE1NDE3MjU4MzQsImV4cCI6MTU0MjE1NzgzNH0.apOpoXOMT5zIQ2HosDmJG0T-NJ0yDScv8_e5Wnf5ZbI");
                    allGames.Add(Task.Run(() => client.PostAsync(
                        "https://mmai-server.siggame.io/games/",
                         new StringContent(serialized, Encoding.UTF8, "application/json"))));
                    Task.WaitAll(allGames.ToArray());
                }
                catch (Exception ex)
                {
                    string idk = ex.Message;
                }
            }
        }

    }
}