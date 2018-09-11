using System;
using System.Collections.Generic;
using System.Text;

namespace TheArena
{
    public class Game
    {
        public List<Player> Competitors;
        public bool IsRunning=false;
        public bool IsComplete=false;
        public int RoundNumber;
        public string Game_URL;
        
        public Game()
        {
            Competitors = new List<Player>();
        }

        public Player GetParentOfPlayersInThisGame()
        {
            if (Competitors.Count > 0)
            {
                return Competitors[0].ParentNode;
            }
            return null;
        }

        public void SetWinner(Player champion, Tournament t, string name, string url)
        {
            IsComplete = true;
            IsRunning = false;
            Game_URL = url;
            if (Competitors.Count > 0)
            {
                Competitors[0].ParentNode.Name=name;
            }
            champion.Traverse(t.GetGames);
        }

        public bool ContainsPlayer(Player p)
        {
            return Competitors.Contains(p);
        }

        public void AddPlayer(Player p)
        {
            Competitors.Add(p);
        }
    }
}
