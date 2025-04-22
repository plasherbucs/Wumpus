using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WumpusAdventure.Helpers;
using WumpusAdventure.Models;

namespace WumpusAdventure.Game
{
    public class AICompanion : Player
    {
        public string Personality { get; set; }
        private string _ollamaModel = "llama3"; 
        private string _ollamaEndpoint = "http://localhost:11434/api/generate"; 

        public AICompanion(string name, string currentRoomId) : base(name, currentRoomId)
        {
            IsAI = true;
            Personality = "A wise and sometimes sarcastic guide who knows about the Wumpus.";

            
            var config = ConfigurationHelper.ReadConfiguration();
            if (config?.AISettings != null)
            {
                _ollamaModel = config.AISettings.Model ?? _ollamaModel;
                _ollamaEndpoint = config.AISettings.Endpoint ?? _ollamaEndpoint;
            }
        }

        public async Task<string> GetAdvice(GameState gameState)
        {
            try
            {
                var currentRoom = gameState.Map[CurrentRoomId];
                var nearbyRooms = new List<Room>();

                foreach (var roomId in currentRoom.Connections.Values)
                {
                    nearbyRooms.Add(gameState.Map[roomId]);
                }

                bool wumpusNearby = nearbyRooms.Exists(room => room.HasWumpus);
                bool pitNearby = nearbyRooms.Exists(room => room.HasPit);
                bool batsNearby = nearbyRooms.Exists(room => room.HasBats);

                
                StringBuilder context = new StringBuilder();
                context.Append($"You are {Name}, {Personality}. ");
                context.Append($"We are in {currentRoom.Name}: {currentRoom.Description}. ");
                context.Append($"The human player has {gameState.Player?.Inventory?["arrows"] ?? 0} arrows left. ");

                if (wumpusNearby)
                    context.Append("You sense the Wumpus is nearby. ");
                if (pitNearby)
                    context.Append("You feel a draft from a pit in a nearby room. ");
                if (batsNearby)
                    context.Append("You hear the fluttering of bats in a nearby room. ");

                context.Append("The player asks for your advice on what to do next. Provide a short, helpful suggestion.");

                return await GetOllamaResponse(context.ToString());
            }
            catch (Exception e)
            {
                return $"ERROR: {e.Message}";
            }
        }

        private async Task<string> GetOllamaResponse(string context)
        {
            using (var httpClient = new HttpClient())
            {
                var requestData = new
                {
                    model = _ollamaModel,
                    prompt = $"{context}\n\nWhat should I do?",
                    stream = false,
                    options = new
                    {
                        temperature = 0.7,
                        num_predict = 150
                    }
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                try
                {
                    
                    var response = await httpClient.PostAsync(_ollamaEndpoint, jsonContent);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(responseString))
                    {
                        
                        using (JsonDocument doc = JsonDocument.Parse(responseString))
                        {
                            JsonElement root = doc.RootElement;

                            if (root.TryGetProperty("response", out JsonElement responseElement))
                            {
                                string? aiResponse = responseElement.GetString();
                                return aiResponse ?? "I'm not sure what to suggest right now.";
                            }

                            return "I couldn't interpret the response correctly.";
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Ollama API Error: {response.StatusCode}");
                        Console.WriteLine(responseString);

                        if (responseString.Contains("connection refused"))
                        {
                            return "ERROR: Connection to Ollama refused. Make sure the Docker container is running.";
                        }
                        else if (responseString.Contains("model not found"))
                        {
                            return $"ERROR: The model '{_ollamaModel}' is not available. Pull the model using 'ollama pull {_ollamaModel}' first.";
                        }

                        return $"ERROR: Ollama API call failed with status {response.StatusCode}.";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ollama API Call Error: {ex.Message}");

                    if (ex.Message.Contains("No connection could be made because the target machine actively refused it"))
                    {
                        return "ERROR: Cannot connect to Ollama. Make sure the Docker container is running on port 11434.";
                    }

                    return $"ERROR: {ex.Message}";
                }
            }
        }
    }
}