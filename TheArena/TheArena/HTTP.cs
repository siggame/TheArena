using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TheArena
{
    public class HTTP
    {
        public static async void HTTPPost(string status, string winReason, string loseReason, string logURL, string winnerTeamName, string winnerVersion, string loserTeamName, string loserVersion)
        {
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
                "}";
            using (var client = new HttpClient())
            {
                try
                {
                    List<Task> allGames = new List<Task>();
                    allGames.Add(Task.Run(() => client.PostAsync(
                        "https://mmai-server.dillonhess.me/games/",
                         new StringContent(myJson, Encoding.UTF8, "application/json"))));       
                    Task.WaitAll(allGames.ToArray());
                }
                catch(Exception ex)
                {
                    string idk = ex.Message;
                }
            }
        }

    }
}
