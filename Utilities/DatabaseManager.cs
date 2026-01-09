using System.Text.Json;
using AdamTheWoo.Models;
using Microsoft.Extensions.Logging;

namespace AdamTheWoo.Utilities;

public class DatabaseManager
{
    private readonly string _dbPath;
    private readonly ILogger _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public DatabaseManager(string dbPath = "data/videos.json", ILogger? logger = null)
    {
        _dbPath = dbPath;
        _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<DatabaseManager>();
    }

    public VideosDatabase LoadDatabase()
    {
        if (!File.Exists(_dbPath))
        {
            _logger.LogWarning("Database file not found, returning empty structure");
            return new VideosDatabase
            {
                ChannelInfo = new ChannelInfo
                {
                    ChannelName = "Adam The Woo",
                    ChannelHandle = "@TheDailyWoo"
                },
                Videos = new List<Video>()
            };
        }

        try
        {
            var json = File.ReadAllText(_dbPath);
            var database = JsonSerializer.Deserialize<VideosDatabase>(json, JsonOptions);
            return database ?? throw new Exception("Failed to deserialize database");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading videos database: {ex.Message}");
        }
    }

    public void SaveDatabase(VideosDatabase database)
    {
        try
        {
            // Create data directory if it doesn't exist
            var directory = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Update last_updated timestamp
            database.ChannelInfo.LastUpdated = DateTime.UtcNow.ToString("O");

            var json = JsonSerializer.Serialize(database, JsonOptions);
            File.WriteAllText(_dbPath, json);

            _logger.LogInformation("Database saved successfully to {Path}", _dbPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error saving videos database: {ex.Message}");
        }
    }

    public void CreateBackup()
    {
        if (File.Exists(_dbPath))
        {
            var backupPath = $"{_dbPath}.backup";
            File.Copy(_dbPath, backupPath, true);
            _logger.LogInformation("Created backup: {BackupPath}", backupPath);
        }
    }
}
