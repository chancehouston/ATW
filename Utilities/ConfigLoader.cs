using AdamTheWoo.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AdamTheWoo.Utilities;

public static class ConfigLoader
{
    public static AppConfig LoadConfig(string configPath = "config/config.yaml")
    {
        try
        {
            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<AppConfig>(yaml);
            return config ?? throw new Exception("Configuration is null");
        }
        catch (FileNotFoundException)
        {
            throw new Exception($"Configuration file not found: {configPath}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error parsing configuration file: {ex.Message}");
        }
    }

    public static string GetEnvironmentVariable(string name, bool required = true)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (required && string.IsNullOrEmpty(value))
        {
            throw new Exception($"Required environment variable not set: {name}");
        }

        return value ?? string.Empty;
    }
}
