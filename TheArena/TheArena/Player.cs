using System;
using System.Collections.Generic;
using System.Text;

namespace TheArena
{
    public class Player : GenericTree<Player> // concrete derivation 
    { 
        public string Name { get; set; }

        public Player ParentNode { get; set; }

        public Player(string name)
        {
            Name = name;
        }
    }
}
