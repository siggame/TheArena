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
        public Winner winner;
        public Loser loser;
    }

    public class Winner
    {
        public string teamName;
        public string version;
    }

    public class Loser
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
            MyPacket p = new MyPacket() { status = status, loseReason = loseReason, winReason = winReason, logUrl = logURL, winner=new Winner() { teamName = winnerTeamName, version = winnerVersion },loser=new Loser() { teamName = loserTeamName, version = loserVersion } };
            string serialized = JsonConvert.SerializeObject(p);
            Console.WriteLine(serialized);
            using (var client = new HttpClient())
            {
                try
                {
                    List<Task> allGames = new List<Task>();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VybmFtZSI6IklMaWtlU29ja3NPblN1bmRheXMiLCJpZCI6Mywicm9sZSI6InVzZXIiLCJpYXQiOjE1NDE4NjY1NTcsImV4cCI6MTU0MjI5ODU1N30.XWaWB_cWhUFEC1m0GxFJ4ln8uq5h086gXGxRmOLVXA0");
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
