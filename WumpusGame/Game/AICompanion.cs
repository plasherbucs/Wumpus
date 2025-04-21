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
        private static string? _openAiApiKey = null;

        public AICompanion(string name, string currentRoomId) : base(name, currentRoomId)
        {
            IsAI = true;
            Personality = "A wise and sometimes sarcastic guide who knows about the Wumpus.";
        }

        private string? GetOpenAIApiKey()
        {
            if (_openAiApiKey == null)
            {
                try
                {
                    var config = ConfigurationHelper.ReadConfiguration();
                    _openAiApiKey = config?.Keys?.OpenAI;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading API key: {ex.Message}");
                }
            }

            return _openAiApiKey;
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

                string? apiKey = GetOpenAIApiKey();

                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("Error: No OpenAI API key found. Please check your configuration.");
                    return "ERROR: No OpenAI API key configured. I cannot provide advice.";
                }

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

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                    var requestData = new
                    {
                        model = "gpt-4o-mini",
                        messages = new[]
                        {
                            new { role = "system", content = context.ToString() },
                            new { role = "user", content = "What should I do?" }
                        },
                        max_tokens = 150
                    };

                    var jsonContent = new StringContent(
                        JsonSerializer.Serialize(requestData),
                        Encoding.UTF8,
                        "application/json");

                    try
                    {
                        var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);
                        var responseString = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(responseString))
                        {
                            using (JsonDocument doc = JsonDocument.Parse(responseString))
                            {
                                JsonElement root = doc.RootElement;
                                if (root.TryGetProperty("choices", out JsonElement choices) &&
                                    choices.GetArrayLength() > 0)
                                {
                                    JsonElement firstChoice = choices[0];
                                    if (firstChoice.TryGetProperty("message", out JsonElement message) &&
                                        message.TryGetProperty("content", out JsonElement contentElement))
                                    {
                                        string? aiResponse = contentElement.GetString();
                                        return aiResponse ?? "I'm not sure what to suggest right now.";
                                    }
                                }
                                return "I couldn't interpret the API response correctly.";
                            }
                        }
                        else
                        {
                            Console.WriteLine($"API Error: {response.StatusCode}");
                            Console.WriteLine(responseString);
                            return $"ERROR: API call failed with status {response.StatusCode}. Please check the console for details.";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"API Call Error: {ex.Message}");
                        return $"ERROR: Exception during API call: {ex.Message}";
                    }
                }
            }
            catch (Exception e)
            {
                return $"ERROR: General exception: {e.Message}";
            }
        }
    }
}