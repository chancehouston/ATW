using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using AdamTheWoo.Models;
using AdamTheWoo.Utilities;
using Microsoft.Extensions.Logging;

namespace AdamTheWoo.Services;

public class YouTubeService
{
    private readonly YouTubeService _youtube;
    private readonly AppConfig _config;
    private readonly ILogger _logger;

    public YouTubeService(string apiKey, AppConfig config, ILogger logger)
    {
        _youtube = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = apiKey,
            ApplicationName = "AdamTheWoo-OnThisDay"
        });
        _config = config;
        _logger = logger;
    }

    public async Task<string> GetChannelIdFromHandle(string handle)
    {
        try
        {
            handle = handle.TrimStart('@');

            var searchRequest = _youtube.Search.List("snippet");
            searchRequest.Q = handle;
            searchRequest.Type = "channel";
            searchRequest.MaxResults = 5;

            var response = await searchRequest.ExecuteAsync();

            // Find exact match
            foreach (var item in response.Items)
            {
                var customUrl = item.Snippet.CustomUrl?.ToLower() ?? "";
                if (customUrl == $"@{handle.ToLower()}")
                {
                    return item.Snippet.ChannelId;
                }
            }

            // Return first result if no exact match
            if (response.Items.Count > 0)
            {
                return response.Items[0].Snippet.ChannelId;
            }

            throw new Exception($"Channel not found: {handle}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error finding channel: {ex.Message}");
        }
    }

    public async Task<(string PlaylistId, string ChannelTitle)> GetChannelUploadsPlaylist(string channelId)
    {
        try
        {
            var channelRequest = _youtube.Channels.List("contentDetails,snippet");
            channelRequest.Id = channelId;

            var response = await channelRequest.ExecuteAsync();

            if (response.Items.Count == 0)
            {
                throw new Exception($"Channel not found: {channelId}");
            }

            var channel = response.Items[0];
            var uploadsPlaylistId = channel.ContentDetails.RelatedPlaylists.Uploads;
            var channelTitle = channel.Snippet.Title;

            return (uploadsPlaylistId, channelTitle);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting channel info: {ex.Message}");
        }
    }

    public async Task<List<Video>> FetchAllVideosFromPlaylist(string playlistId)
    {
        var videos = new List<Video>();
        string? nextPageToken = null;
        int pageCount = 0;

        _logger.LogInformation("Fetching videos from playlist: {PlaylistId}", playlistId);

        do
        {
            try
            {
                var playlistRequest = _youtube.PlaylistItems.List("snippet,contentDetails");
                playlistRequest.PlaylistId = playlistId;
                playlistRequest.MaxResults = 50;
                playlistRequest.PageToken = nextPageToken;

                var response = await playlistRequest.ExecuteAsync();
                pageCount++;

                _logger.LogInformation("Page {PageCount}: Found {Count} videos", pageCount, response.Items.Count);

                foreach (var item in response.Items)
                {
                    var snippet = item.Snippet;

                    // Skip private/deleted videos
                    if (snippet.Title == "Private video" || snippet.Title == "Deleted video")
                        continue;

                    var videoId = snippet.ResourceId.VideoId;
                    var title = snippet.Title;
                    var description = snippet.Description ?? "";
                    var uploadDate = snippet.PublishedAt?.ToString("yyyy-MM-dd") ?? "";

                    // Try to parse recording date from title
                    string? recordingDate = null;
                    var dateObj = DateHelper.ParseDateFromTitle(title, _config);

                    if (dateObj.HasValue)
                    {
                        recordingDate = dateObj.Value.ToString("yyyy-MM-dd");
                    }
                    else if (_config.DateParsing.UseUploadDateFallback)
                    {
                        recordingDate = uploadDate;
                    }

                    var video = new Video
                    {
                        VideoId = videoId,
                        Title = title,
                        Description = description,
                        UploadDate = uploadDate,
                        RecordingDate = recordingDate,
                        Url = $"https://www.youtube.com/watch?v={videoId}",
                        Thumbnail = snippet.Thumbnails?.High?.Url ?? ""
                    };

                    videos.Add(video);
                }

                nextPageToken = response.NextPageToken;

                // Be nice to the API
                if (nextPageToken != null)
                {
                    await Task.Delay(500);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching videos: {Error}", ex.Message);
                break;
            }
        } while (nextPageToken != null);

        _logger.LogInformation("Total videos fetched: {Count}", videos.Count);
        return videos;
    }
}
