using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml;

namespace TheArena
{
    public class HTTP
    {
        public static async void HTTPPost(string status, string winReason, string loseReason, string logURL, string winnerTeamName, string winnerVersion, string loserTeamName, string loserVersion)
        {
            string myJson =
                "{" +
                "'status': '" + status + "'," +
                "'winReason': '" + winReason + "'," +
                "'loseReason': '" + loseReason + "'," +
                "'logUrl': '" + logURL + "'," +
                "'winner': {" +
                    "'teamName:'" + winnerTeamName + "'," +
                    "'version:'" + winnerVersion + "'" +
                "}," +
                "'loser': {" +
                    "'teamName:'" + loserTeamName + "'," +
                    "'version:'" + loserVersion + "'" +
                "}" +
                "}";
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(
                    "http://mmai-server.dillonhess.me",
                     new StringContent(myJson, Encoding.UTF8, "application/json"));
            }
        }

    }
}
