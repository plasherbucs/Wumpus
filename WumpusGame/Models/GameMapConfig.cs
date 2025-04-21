using System.Collections.Generic;

namespace WumpusAdventure.Models
{
    public class GameMapConfig
    {
        public Dictionary<string, Room>? Map { get; set; }
    }
}