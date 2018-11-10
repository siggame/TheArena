using Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheArena
{
    public static class TournamentExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class Tournament : GenericTree<Player>
    {

        static List<PlayerInfo> unfilledCompetitors;
        static List<Game> games = new List<Game>();
        int competitor_slots_in_bracket = 0;
        static int Rounds;
        Player champion;
        public bool IsDone { get; set; }

        public void GetNextNonRunningGame(out Game toReturn)
        {
            try
            {
                Log.TraceMessage(Log.Nav.NavIn, "Getting Next NonRunning Game ", Log.LogType.Info);
                champion.Traverse(PrettyPrint);
                Thread.Sleep(1000);
                bool allGamesComplete = true;
                for (int i = 0; i < games.Count; i++)
                {
                    Log.TraceMessage(Log.Nav.NavIn, "There are " + games.Count + " games.", Log.LogType.Info);
                    if (!games[i].IsComplete)
                    {
                        Log.TraceMessage(Log.Nav.NavIn, "Game " + i + " is not done and has "+games[i].Competitors.Count()+" players.", Log.LogType.Info);
                        allGamesComplete = false;
                        if (games[i].Competitors.Count() == 1 && games[i].Competitors[0].Info.TeamName!="")
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "Setting winner for single player match->"+games[i].Competitors[0].Info.TeamName, Log.LogType.Info);
                            games[i].SetWinner(games[i].Competitors[0], this, games[i].Competitors[0].Info.TeamName, "");
                        }
                        else if (!games[i].IsRunning && games[i].Competitors.Count() == 2 && games[i].Competitors[0].Info.TeamName.ToUpper() == "BYE")
                        {
                            games[i].SetWinner(games[i].Competitors[1], this, games[i].Competitors[1].Info.TeamName, "");
                        }
                        else if (!games[i].IsRunning && games[i].Competitors.Count() == 2 && games[i].Competitors[1].Info.TeamName.ToUpper() == "BYE")
                        {
                            games[i].SetWinner(games[i].Competitors[1], this, games[i].Competitors[0].Info.TeamName, "");
                        }
                        else
                        {
                            if (!games[i].IsRunning && games[i].Competitors[0].Info.TeamName != "" && games[i].Competitors[1].Info.TeamName != "")
                            {
                                Log.TraceMessage(Log.Nav.NavIn, "Game " + i + " is being returned. The first teamname is " + games[i].Competitors[0].Info.TeamName, Log.LogType.Info);
                                Log.TraceMessage(Log.Nav.NavIn, "Game " + i + " is being returned. The second teamname is " + games[i].Competitors[1].Info.TeamName, Log.LogType.Info);
                                toReturn = games[i];
                                return;
                            }
                            else if (games[i].Competitors.Count == 1 && games[i].Competitors[0].Info.TeamName != "")
                            {
                                Console.WriteLine("Winner: " + games[i].Competitors[0].Info.TeamName);
                                Log.TraceMessage(Log.Nav.NavIn, "Winner decided: " + games[i].Competitors[0].Info.TeamName, Log.LogType.Info);
                                allGamesComplete = true;
                            }
                        }
                    }
                }
                if (allGamesComplete)
                {
                    IsDone = true;
                }
            }
            catch (Exception ex)
            {
                Log.TraceMessage(Log.Nav.NavOut, ex);
                Console.WriteLine(ex);
            }
            toReturn = null;
        }

        public Tournament(List<PlayerInfo> competitors, int players_per_game)
        {
            IsDone = false;
            while (competitors.Count % players_per_game != 0)
            {
                PlayerInfo pi = new PlayerInfo() { TeamName = "BYE", Submission = "-1", Lang = Languages.None };
                competitors.Add(pi);
            }
            int number_of_rounds = (int)Math.Ceiling(Math.Log(competitors.Count) / Math.Log(players_per_game));
            TournamentExtensions.Shuffle(competitors);
            champion = new Player(new PlayerInfo() { Lang = Languages.None, Submission = "0", TeamName = "" }) { ParentNode = null };
            Rounds = number_of_rounds;
            unfilledCompetitors = competitors;
            AddLowerLevelRoundGame(champion, number_of_rounds, players_per_game);
            champion.Traverse(AddCompetitorsToBottomRound);
            champion.Traverse(PrettyPrint);
            champion.Traverse(GetGames);
        }

        public void AddLowerLevelRoundGame(Player parent, int round_number, int players_per_game)
        {
            if (round_number <= 0)
            {
                return;
            }
            else
            {
                for (int i = 0; i < players_per_game && competitor_slots_in_bracket < unfilledCompetitors.Count; i++)
                {
                    Player g = new Player(new PlayerInfo() { Lang = Languages.None, Submission = "0", TeamName = "" });
                    AddLowerLevelRoundGame(g, round_number - 1, players_per_game);
                    g.ParentNode = parent;
                    parent.AddChild(g);
                    if (round_number == 1)
                    {
                        competitor_slots_in_bracket++;
                    }
                }
            }
        }

        public void GetGames(int depth, Player node)
        {
            bool handled = false;
            for (int i = 0; i < games.Count; i++)
            {
                if (games[i].GetParentOfPlayersInThisGame() == node.ParentNode)
                {
                    if (!games[i].ContainsPlayer(node))
                    {
                        games[i].AddPlayer(node);
                    }
                    handled = true;
                    break;
                }
            }
            if (!handled)
            {
                Game g = new Game();
                g.AddPlayer(node);
                games.Add(g);
                g.RoundNumber = Rounds - depth + 1;
            }
        }

        static void AddCompetitorsToBottomRound(int depth, Player node)
        {
            if (depth == Rounds)
            {
                node.Info = unfilledCompetitors[0];
                unfilledCompetitors.RemoveAt(0);
            }
        }

        static void PrettyPrint(int depth, Player node)
        {                                // a little one-line string-concatenation (n-times)
            Console.WriteLine("{0}{1}: {2}", String.Join("   ", new string[depth + 1]), depth, node.Info.TeamName);
        }
    }
}
