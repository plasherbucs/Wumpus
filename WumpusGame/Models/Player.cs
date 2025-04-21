using System.Collections.Generic;

namespace WumpusAdventure.Models
{
    public class Player
    {
        public string Name { get; set; }
        public string CurrentRoomId { get; set; }
        public Dictionary<string, int> Inventory { get; set; }
        public int Health { get; set; }
        public bool IsAI { get; set; }

        public Player(string name, string currentRoomId)
        {
            Name = name;
            CurrentRoomId = currentRoomId;
            Inventory = new Dictionary<string, int> { { "arrows", 3 } };
            Health = 100;
            IsAI = false;
        }
    }
}