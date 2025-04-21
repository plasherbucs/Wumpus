using System.Collections.Generic;

namespace WumpusAdventure.Models
{
    public class Room
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Connections { get; set; }
        public bool HasWumpus { get; set; }
        public bool HasPit { get; set; }
        public bool HasBats { get; set; }

        public Room(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
            Connections = new Dictionary<string, string>();
        }

        public void AddConnection(string direction, string roomId)
        {
            Connections[direction] = roomId;
        }
    }
}