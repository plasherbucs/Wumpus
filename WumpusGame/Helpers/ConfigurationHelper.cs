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
                string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

                if (File.Exists(configPath))
                {
                    string jsonContent = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<ApiConfiguration>(jsonContent);
                }
                else
                {
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
        public KeyStorage? Keys { get; set; }
        public PathStorage? Paths { get; set; }

        public class KeyStorage
        {
            public string? OpenAI { get; set; }
        }

        public class PathStorage
        {
            public string? GameMap { get; set; }
        }
    }
}