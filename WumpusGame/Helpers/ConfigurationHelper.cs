using System;
using System.IO;
using System.Text.Json;

namespace WumpusAdventure.Helpers
{
    public static class ConfigurationHelper
    {
        public static ApiConfiguration? ReadConfiguration()
        {
            try
            {
                string[] possiblePaths = new string[]
                {
                    "appsettings.json",
                    Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json")
                };

                string? configPath = null;
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        configPath = path;
                        break;
                    }
                }

                if (configPath != null)
                {
                    string jsonContent = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<ApiConfiguration>(jsonContent);
                }
                else
                {
                    Console.WriteLine("Warning: Configuration file not found in any of the expected locations.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading configuration: {ex.Message}");
                return null;
            }
        }
    }

    public class ApiConfiguration
    {
        public PathStorage? Paths { get; set; }
        public AISettingsStorage? AISettings { get; set; }

        public class PathStorage
        {
            public string? GameMap { get; set; }
        }

        public class AISettingsStorage
        {
            public string? Model { get; set; } = "phi3:mini";
            public string? Endpoint { get; set; } = "http://localhost:11434/api/generate";
        }
    }
}