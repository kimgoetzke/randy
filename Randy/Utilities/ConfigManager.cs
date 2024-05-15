namespace Randy.Utilities;

using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public static class ConfigManager
{
    private static ILogger logger => LoggerProvider.CreateLogger(nameof(ConfigManager));
    private static JsonSerializerOptions jsonOptions =>
        new() { PropertyNameCaseInsensitive = true };

    public static Config Load()
    {
        Config config;
        if (File.Exists(Constants.ConfigFile))
        {
            try
            {
                var json = File.ReadAllText(Constants.ConfigFile);
                config = JsonSerializer.Deserialize<Config>(json, jsonOptions)!;
                logger.LogInformation("Loaded {F} successfully", Constants.ConfigFile);
            }
            catch (Exception e)
            {
                config = new Config();
                logger.LogError(
                    "Created new {F} because an error occurred when loading the file: {Message}",
                    Constants.ConfigFile,
                    e.Message
                );
            }
        }
        else
        {
            config = new Config();
            logger.LogInformation("Created new {F} because it did not exist", Constants.ConfigFile);
        }
        return config;
    }

    public static void Save(Config config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(Constants.ConfigFile, json);
        }
        catch (Exception e)
        {
            logger.LogError("Failed to save config: {Message}", e.Message);
        }
    }
}
