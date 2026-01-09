using AdamTheWoo.Models;
using AdamTheWoo.Services;
using AdamTheWoo.Utilities;
using DotNetEnv;
using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

namespace AdamTheWoo;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Load .env file if it exists
        if (File.Exists(".env"))
        {
            Env.Load();
        }

        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].ToLower();

        try
        {
            return command switch
            {
                "fetch-videos" => await FetchVideos(),
                "update-playlist" => await UpdatePlaylist(),
                "post-to-reddit" => await PostToReddit(args.Length > 1 ? args[1] : null),
                "help" or "--help" or "-h" => PrintUsage(),
                _ => InvalidCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Adam The Woo - On This Day Memorial Project");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  fetch-videos       Fetch all videos from YouTube channel and build database");
        Console.WriteLine("  update-playlist    Update YouTube playlist with today's 'on this day' videos");
        Console.WriteLine("  post-to-reddit     Post today's videos to Reddit");
        Console.WriteLine("  help               Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run fetch-videos");
        Console.WriteLine("  dotnet run update-playlist");
        Console.WriteLine("  dotnet run post-to-reddit");
    }

    static int InvalidCommand(string command)
    {
        Console.WriteLine($"Unknown command: {command}");
        Console.WriteLine("Run 'dotnet run help' for usage information");
        return 1;
    }

    static async Task<int> FetchVideos()
    {
        // Load configuration
        var config = ConfigLoader.LoadConfig();
        var logger = CreateLogger(config);

        logger.LogInformation(new string('=', 60));
        logger.LogInformation("Adam The Woo - On This Day: Video Fetch");
        logger.LogInformation(new string('=', 60));

        // Get API key
        var apiKey = ConfigLoader.GetEnvironmentVariable("YOUTUBE_API_KEY");
        var channelHandle = Environment.GetEnvironmentVariable("YOUTUBE_CHANNEL_HANDLE") ?? config.Channel.Handle;

        // Create YouTube service
        logger.LogInformation("Connecting to YouTube API...");
        var youtubeService = new YouTubeService(apiKey, config, logger);

        // Get channel ID
        logger.LogInformation("Finding channel: {Handle}", channelHandle);
        var channelId = await youtubeService.GetChannelIdFromHandle(channelHandle);
        logger.LogInformation("Channel ID: {ChannelId}", channelId);

        // Get uploads playlist
        logger.LogInformation("Getting uploads playlist...");
        var (uploadsPlaylistId, channelTitle) = await youtubeService.GetChannelUploadsPlaylist(channelId);
        logger.LogInformation("Channel: {Title}", channelTitle);
        logger.LogInformation("Uploads Playlist ID: {PlaylistId}", uploadsPlaylistId);

        // Fetch all videos
        var videos = await youtubeService.FetchAllVideosFromPlaylist(uploadsPlaylistId);

        // Backup existing database
        var dbManager = new DatabaseManager("data/videos.json", logger);
        dbManager.CreateBackup();

        // Create database structure
        var database = new VideosDatabase
        {
            ChannelInfo = new ChannelInfo
            {
                ChannelId = channelId,
                ChannelName = channelTitle,
                ChannelHandle = channelHandle,
                UploadsPlaylistId = uploadsPlaylistId,
                TotalVideos = videos.Count,
                LastUpdated = DateTime.UtcNow.ToString("O")
            },
            Videos = videos
        };

        // Save database
        logger.LogInformation("Saving videos database...");
        dbManager.SaveDatabase(database);

        // Statistics
        logger.LogInformation("");
        logger.LogInformation(new string('=', 60));
        logger.LogInformation("FETCH COMPLETE");
        logger.LogInformation(new string('=', 60));
        logger.LogInformation("Total videos: {Count}", videos.Count);

        var videosWithDates = videos.Count(v => !string.IsNullOrEmpty(v.RecordingDate));
        logger.LogInformation("Videos with recording dates: {Count}", videosWithDates);
        logger.LogInformation("Videos without dates: {Count}", videos.Count - videosWithDates);

        // Date range
        if (videosWithDates > 0)
        {
            var dates = videos
                .Where(v => !string.IsNullOrEmpty(v.RecordingDate))
                .Select(v => v.RecordingDate!)
                .OrderBy(d => d)
                .ToList();
            logger.LogInformation("Date range: {Start} to {End}", dates.First(), dates.Last());
        }

        logger.LogInformation("");
        logger.LogInformation("Database saved to: data/videos.json");
        logger.LogInformation("You can now run 'update-playlist' to create today's playlist!");

        return 0;
    }

    static async Task<int> UpdatePlaylist()
    {
        // Load configuration
        var config = ConfigLoader.LoadConfig();
        var logger = CreateLogger(config);

        var dryRun = config.Features.DryRun;

        logger.LogInformation(new string('=', 60));
        logger.LogInformation("Adam The Woo - On This Day: Playlist Update");
        if (dryRun)
        {
            logger.LogInformation("*** DRY RUN MODE - NO CHANGES WILL BE MADE ***");
        }
        logger.LogInformation(new string('=', 60));

        // Load videos database
        logger.LogInformation("Loading videos database...");
        var dbManager = new DatabaseManager("data/videos.json", logger);
        var database = dbManager.LoadDatabase();

        if (database.Videos.Count == 0)
        {
            logger.LogError("No videos in database. Run 'fetch-videos' first!");
            return 1;
        }

        // Get today's date
        var timezone = config.DateParsing.Timezone;
        var (month, day, formattedDate) = DateHelper.GetTodayDate(timezone);
        logger.LogInformation("Today's date: {Date} (Month: {Month}, Day: {Day})", formattedDate, month, day);

        // Find videos for today
        var todayVideos = DateHelper.GetVideosForDate(database, month, day);
        logger.LogInformation("Found {Count} videos for this date", todayVideos.Count);

        if (todayVideos.Count == 0)
        {
            logger.LogInformation("No videos found for today. Exiting.");
            return 0;
        }

        // Display videos
        foreach (var video in todayVideos)
        {
            var year = video.RecordingDate?[..4] ?? "Unknown";
            logger.LogInformation("  - {Year}: {Title}", year, video.Title);
        }

        if (dryRun)
        {
            logger.LogInformation("Dry run mode - would have updated playlist with these videos");
            return 0;
        }

        if (!config.Features.CreatePlaylist)
        {
            logger.LogInformation("Playlist creation disabled in config");
            return 0;
        }

        // Authenticate and create/update playlist
        logger.LogInformation("Authenticating with YouTube...");
        var credential = await GetYouTubeCredential();
        var playlistService = new PlaylistService(credential, logger);

        // Format playlist title and description
        var playlistTitle = config.Playlist.TitleFormat
            .Replace("{month}", formattedDate.Split()[0])
            .Replace("{day}", formattedDate.Split()[1]);

        var playlistDescription = config.Playlist.DescriptionFormat
            .Replace("{month}", formattedDate.Split()[0])
            .Replace("{day}", formattedDate.Split()[1])
            .Replace("{date}", formattedDate);

        // Find or create playlist
        logger.LogInformation("Managing playlist: {Title}", playlistTitle);
        var playlistId = await playlistService.FindOrCreatePlaylist(playlistTitle, playlistDescription);

        // Clear existing videos
        logger.LogInformation("Clearing existing playlist items...");
        await playlistService.ClearPlaylist(playlistId);

        // Add today's videos
        logger.LogInformation("Adding videos to playlist...");
        var videoIds = todayVideos.Select(v => v.VideoId).ToList();
        var added = await playlistService.AddVideosToPlaylist(playlistId, videoIds);

        // Results
        var playlistUrl = $"https://www.youtube.com/playlist?list={playlistId}";

        logger.LogInformation("");
        logger.LogInformation(new string('=', 60));
        logger.LogInformation("UPDATE COMPLETE");
        logger.LogInformation(new string('=', 60));
        logger.LogInformation("Playlist: {Title}", playlistTitle);
        logger.LogInformation("Videos added: {Added}/{Total}", added, todayVideos.Count);
        logger.LogInformation("Playlist URL: {Url}", playlistUrl);
        logger.LogInformation("");

        return 0;
    }

    static async Task<int> PostToReddit(string? playlistUrl)
    {
        // Load configuration
        var config = ConfigLoader.LoadConfig();
        var logger = CreateLogger(config);

        var dryRun = config.Features.DryRun;

        logger.LogInformation(new string('=', 60));
        logger.LogInformation("Adam The Woo - On This Day: Reddit Post");
        if (dryRun)
        {
            logger.LogInformation("*** DRY RUN MODE - NO POST WILL BE MADE ***");
        }
        logger.LogInformation(new string('=', 60));

        if (!config.Features.PostToReddit)
        {
            logger.LogInformation("Reddit posting disabled in config");
            return 0;
        }

        // Load videos database
        logger.LogInformation("Loading videos database...");
        var dbManager = new DatabaseManager("data/videos.json", logger);
        var database = dbManager.LoadDatabase();

        if (database.Videos.Count == 0)
        {
            logger.LogError("No videos in database. Run 'fetch-videos' first!");
            return 1;
        }

        // Get today's date
        var timezone = config.DateParsing.Timezone;
        var (month, day, formattedDate) = DateHelper.GetTodayDate(timezone);
        logger.LogInformation("Today's date: {Date}", formattedDate);

        // Find videos for today
        var todayVideos = DateHelper.GetVideosForDate(database, month, day);
        logger.LogInformation("Found {Count} videos for this date", todayVideos.Count);

        if (todayVideos.Count == 0)
        {
            logger.LogInformation("No videos found for today. No post will be made.");
            return 0;
        }

        // Format post content
        var postTitle = config.Reddit.PostTitleFormat
            .Replace("{month}", formattedDate.Split()[0])
            .Replace("{day}", formattedDate.Split()[1])
            .Replace("{date}", formattedDate);

        var videoList = DateHelper.FormatVideoListForReddit(todayVideos);

        var postBody = config.Reddit.PostTemplate
            .Replace("{month}", formattedDate.Split()[0])
            .Replace("{day}", formattedDate.Split()[1])
            .Replace("{date}", formattedDate)
            .Replace("{video_list}", videoList)
            .Replace("{playlist_url}", playlistUrl ?? "Coming soon!");

        // Preview
        logger.LogInformation("");
        logger.LogInformation("Post preview:");
        logger.LogInformation(new string('-', 60));
        logger.LogInformation("Title: {Title}", postTitle);
        logger.LogInformation("");
        logger.LogInformation("{Body}", postBody);
        logger.LogInformation(new string('-', 60));
        logger.LogInformation("");

        if (dryRun)
        {
            logger.LogInformation("Dry run mode - post not submitted");
            return 0;
        }

        // Authenticate with Reddit
        logger.LogInformation("Authenticating with Reddit...");
        var clientId = ConfigLoader.GetEnvironmentVariable("REDDIT_CLIENT_ID");
        var clientSecret = ConfigLoader.GetEnvironmentVariable("REDDIT_CLIENT_SECRET");
        var username = ConfigLoader.GetEnvironmentVariable("REDDIT_USERNAME");
        var password = ConfigLoader.GetEnvironmentVariable("REDDIT_PASSWORD");
        var userAgent = Environment.GetEnvironmentVariable("REDDIT_USER_AGENT") ?? "adam-the-woo-on-this-day-bot/1.0";

        var redditService = new RedditService(clientId, clientSecret, username, password, userAgent, logger);
        logger.LogInformation("Authenticated as: u/{Username}", username);

        // Post to subreddit
        var subredditName = config.Reddit.Subreddit;
        var flairText = config.Reddit.FlairText;

        logger.LogInformation("Posting to r/{Subreddit}...", subredditName);
        var postUrl = redditService.CreatePost(subredditName, postTitle, postBody, flairText);

        // Results
        logger.LogInformation("");
        logger.LogInformation(new string('=', 60));
        logger.LogInformation("POST COMPLETE");
        logger.LogInformation(new string('=', 60));
        logger.LogInformation("Subreddit: r/{Subreddit}", subredditName);
        logger.LogInformation("Post URL: {Url}", postUrl);
        logger.LogInformation("");

        return 0;
    }

    static ILogger CreateLogger(AppConfig config)
    {
        var logLevel = config.Logging.Level.ToUpper() switch
        {
            "DEBUG" => LogLevel.Debug,
            "INFO" => LogLevel.Information,
            "WARNING" => LogLevel.Warning,
            "ERROR" => LogLevel.Error,
            _ => LogLevel.Information
        };

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(logLevel);
        });

        return loggerFactory.CreateLogger<Program>();
    }

    static async Task<UserCredential> GetYouTubeCredential()
    {
        var clientId = Environment.GetEnvironmentVariable("YOUTUBE_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("YOUTUBE_CLIENT_SECRET");
        var refreshToken = Environment.GetEnvironmentVariable("YOUTUBE_REFRESH_TOKEN");

        if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(refreshToken))
        {
            // Use credentials from environment
            return new UserCredential(
                new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow(
                    new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = clientId,
                            ClientSecret = clientSecret
                        },
                        Scopes = new[] { "https://www.googleapis.com/auth/youtube.force-ssl" }
                    }),
                "user",
                new Google.Apis.Auth.OAuth2.Responses.TokenResponse
                {
                    RefreshToken = refreshToken
                });
        }

        // OAuth flow for first-time setup
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = clientId ?? throw new Exception("YOUTUBE_CLIENT_ID not set"),
                ClientSecret = clientSecret ?? throw new Exception("YOUTUBE_CLIENT_SECRET not set")
            },
            new[] { "https://www.googleapis.com/auth/youtube.force-ssl" },
            "user",
            CancellationToken.None,
            new FileDataStore("token.json", true));

        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("IMPORTANT: Save these for environment variables:");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"YOUTUBE_REFRESH_TOKEN={credential.Token.RefreshToken}");
        Console.WriteLine(new string('=', 60) + "\n");

        return credential;
    }
}
