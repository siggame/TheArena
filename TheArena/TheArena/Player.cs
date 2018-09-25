using System;
using System.Collections.Generic;
using System.Text;

namespace TheArena
{
    public class Player : GenericTree<Player> // concrete derivation 
    { 
        public PlayerInfo Info { get; set; }

        public Player ParentNode { get; set; }


        public Player(PlayerInfo info)
        {
            Info = info;
        }
    }
}
