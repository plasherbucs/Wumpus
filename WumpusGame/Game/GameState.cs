using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WumpusAdventure.Models;

namespace WumpusAdventure.Game
{
    public class GameState
    {
        public Dictionary<string, Room> Map { get; set; } = new Dictionary<string, Room>();
        public Player? Player { get; set; }
        public AICompanion? AICompanion { get; set; }
        public int Turns { get; set; }
        public bool GameOver { get; set; }
        public bool Win { get; set; }

        public void InitializeNewGame(string playerName)
        {
            LoadGameMapFromJson();

            PlaceHazardsRandomly();

            string startingRoomId = "room1";
            Player = new Player(playerName, startingRoomId);
            AICompanion = new AICompanion("Jarvis", startingRoomId);

            if (Map.ContainsKey(startingRoomId))
            {
                Map[startingRoomId].HasWumpus = false;
                Map[startingRoomId].HasPit = false;
                Map[startingRoomId].HasBats = false;
            }
            else
            {
                throw new InvalidOperationException("Starting room 'room1' was not found in the map data!");
            }

            Turns = 0;
            GameOver = false;
            Win = false;

        }

        private void PlaceHazardsRandomly()
        {
            foreach (var room in Map.Values)
            {
                room.HasWumpus = false;
                room.HasPit = false;
            }

            List<Room> eligibleRooms = Map.Values
                .Where(r => r.Id != "room1" && !r.HasBats)
                .ToList();

            if (eligibleRooms.Count < 3)
            {
                Console.WriteLine("Warning: Not enough eligible rooms for all hazards. Some hazards might be missing.");
                return;
            }

            Random random = new Random();

            int wumpusIndex = random.Next(eligibleRooms.Count);
            Room wumpusRoom = eligibleRooms[wumpusIndex];
            wumpusRoom.HasWumpus = true;

            eligibleRooms.RemoveAt(wumpusIndex);

            int pit1Index = random.Next(eligibleRooms.Count);
            Room pit1Room = eligibleRooms[pit1Index];
            pit1Room.HasPit = true;

            eligibleRooms.RemoveAt(pit1Index);

            int pit2Index = random.Next(eligibleRooms.Count);
            Room pit2Room = eligibleRooms[pit2Index];
            pit2Room.HasPit = true;
        }

        private void LoadGameMapFromJson()
        {
            try
            {
                string mapPath = "GameMap.json";
                
                string fullPath = Path.Combine(AppContext.BaseDirectory, mapPath);
                
                Console.WriteLine($"Looking for map at: {fullPath}");
                
                if (!File.Exists(fullPath))
                {
                    fullPath = Path.Combine(Directory.GetCurrentDirectory(), mapPath);
                    Console.WriteLine($"Not found. Trying: {fullPath}");
                    
                    if (!File.Exists(fullPath))
                    {
                        string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
                        fullPath = Path.Combine(projectDir, mapPath);
                        Console.WriteLine($"Not found. Trying project directory: {fullPath}");
                    }
                }

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Map file '{mapPath}' not found. Last attempted path: {fullPath}");
                }

                Console.WriteLine($"Found map at: {fullPath}");
                string jsonContent = File.ReadAllText(fullPath);
                var mapConfig = JsonSerializer.Deserialize<GameMapConfig>(jsonContent);

                if (mapConfig?.Map == null || mapConfig.Map.Count == 0)
                {
                    throw new InvalidOperationException("Invalid map configuration: Map is missing or empty");
                }

                Map = mapConfig.Map;

                ValidateMapConnections();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR: Failed to load map: {ex.Message}");
                Console.WriteLine("Please ensure the map file exists and has the correct format.");
                throw; 
            }
        }

        private void ValidateMapConnections()
        {
            foreach (var room in Map.Values)
            {
                foreach (var connection in room.Connections)
                {
                    if (!Map.ContainsKey(connection.Value))
                    {
                        Console.WriteLine($"WARNING: Room {room.Id} has a connection to non-existent room {connection.Value}");
                    }
                }
            }
        }

        public bool SaveGame(string filename = "wumpus_save.json")
        {
            try
            {
                var saveData = new
                {
                    Map = Map,
                    Player = Player != null ? new
                    {
                        Player.Name,
                        Player.CurrentRoomId,
                        Player.Inventory,
                        Player.Health,
                        Player.IsAI
                    } : null,
                    AICompanion = AICompanion != null ? new
                    {
                        AICompanion.Name,
                        AICompanion.CurrentRoomId,
                        AICompanion.Inventory,
                        AICompanion.Health,
                        AICompanion.IsAI,
                        AICompanion.Personality
                    } : null,
                    Turns,
                    GameOver,
                    Win
                };

                string jsonString = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filename, jsonString);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error saving game: {e.Message}");
                return false;
            }
        }

        public bool LoadGame(string filename = "wumpus_save.json")
        {
            if (!File.Exists(filename))
                return false;

            try
            {
                string jsonString = File.ReadAllText(filename);
                var saveData = JsonSerializer.Deserialize<SaveData>(jsonString);

                if (saveData == null)
                    return false;

                Map.Clear();
                if (saveData.Map != null)
                {
                    foreach (var roomEntry in saveData.Map)
                    {
                        var roomData = roomEntry.Value;
                        if (roomData != null && roomData.Id != null && roomData.Name != null && roomData.Description != null)
                        {
                            var room = new Room(roomData.Id, roomData.Name, roomData.Description)
                            {
                                HasWumpus = roomData.HasWumpus,
                                HasPit = roomData.HasPit,
                                HasBats = roomData.HasBats
                            };

                            if (roomData.Connections != null)
                            {
                                foreach (var connection in roomData.Connections)
                                {
                                    if (!string.IsNullOrEmpty(connection.Key) && !string.IsNullOrEmpty(connection.Value))
                                        room.Connections[connection.Key] = connection.Value;
                                }
                            }

                            Map[roomData.Id] = room;
                        }
                    }
                }

                var playerData = saveData.Player;
                if (playerData != null && playerData.Name != null && playerData.CurrentRoomId != null)
                {
                    Player = new Player(playerData.Name, playerData.CurrentRoomId)
                    {
                        Health = playerData.Health,
                        IsAI = playerData.IsAI
                    };

                    Player.Inventory.Clear();
                    if (playerData.Inventory != null)
                    {
                        foreach (var item in playerData.Inventory)
                        {
                            Player.Inventory[item.Key] = item.Value;
                        }
                    }
                }

                var aiData = saveData.AICompanion;
                if (aiData != null && aiData.Name != null && aiData.CurrentRoomId != null)
                {
                    AICompanion = new AICompanion(aiData.Name, aiData.CurrentRoomId)
                    {
                        Health = aiData.Health,
                        Personality = aiData.Personality ?? "A wise and sometimes sarcastic guide who knows about the Wumpus."
                    };

                    AICompanion.Inventory.Clear();
                    if (aiData.Inventory != null)
                    {
                        foreach (var item in aiData.Inventory)
                        {
                            AICompanion.Inventory[item.Key] = item.Value;
                        }
                    }
                }

                Turns = saveData.Turns;
                GameOver = saveData.GameOver;
                Win = saveData.Win;

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading game: {e.Message}");
                return false;
            }
        }

        private class SaveData
        {
            public Dictionary<string, RoomData>? Map { get; set; }
            public PlayerData? Player { get; set; }
            public AICompanionData? AICompanion { get; set; }
            public int Turns { get; set; }
            public bool GameOver { get; set; }
            public bool Win { get; set; }
        }

        private class RoomData
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public Dictionary<string, string>? Connections { get; set; }
            public bool HasWumpus { get; set; }
            public bool HasPit { get; set; }
            public bool HasBats { get; set; }
        }

        private class PlayerData
        {
            public string? Name { get; set; }
            public string? CurrentRoomId { get; set; }
            public Dictionary<string, int>? Inventory { get; set; }
            public int Health { get; set; }
            public bool IsAI { get; set; }
        }

        private class AICompanionData : PlayerData
        {
            public string? Personality { get; set; }
        }
    }
}