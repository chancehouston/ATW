using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;

namespace AdamTheWoo.Services;

public class PlaylistService
{
    private readonly YouTubeService _youtube;
    private readonly ILogger _logger;

    public PlaylistService(UserCredential credential, ILogger logger)
    {
        _youtube = new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "AdamTheWoo-OnThisDay"
        });
        _logger = logger;
    }

    public async Task<string> FindOrCreatePlaylist(string title, string description)
    {
        try
        {
            // Search for existing playlist
            var listRequest = _youtube.Playlists.List("snippet");
            listRequest.Mine = true;
            listRequest.MaxResults = 50;

            var response = await listRequest.ExecuteAsync();

            foreach (var item in response.Items)
            {
                if (item.Snippet.Title == title)
                {
                    _logger.LogInformation("Found existing playlist: {PlaylistId}", item.Id);
                    return item.Id;
                }
            }

            // Create new playlist if not found
            _logger.LogInformation("Creating new playlist: {Title}", title);

            var newPlaylist = new Playlist
            {
                Snippet = new PlaylistSnippet
                {
                    Title = title,
                    Description = description
                },
                Status = new PlaylistStatus
                {
                    PrivacyStatus = "public"
                }
            };

            var insertRequest = _youtube.Playlists.Insert(newPlaylist, "snippet,status");
            var createdPlaylist = await insertRequest.ExecuteAsync();

            _logger.LogInformation("Created playlist: {PlaylistId}", createdPlaylist.Id);
            return createdPlaylist.Id;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error managing playlist: {ex.Message}");
        }
    }

    public async Task ClearPlaylist(string playlistId)
    {
        try
        {
            var itemIds = new List<string>();
            string? nextPageToken = null;

            do
            {
                var listRequest = _youtube.PlaylistItems.List("id");
                listRequest.PlaylistId = playlistId;
                listRequest.MaxResults = 50;
                listRequest.PageToken = nextPageToken;

                var response = await listRequest.ExecuteAsync();

                foreach (var item in response.Items)
                {
                    itemIds.Add(item.Id);
                }

                nextPageToken = response.NextPageToken;
            } while (nextPageToken != null);

            // Delete each item
            foreach (var itemId in itemIds)
            {
                await _youtube.PlaylistItems.Delete(itemId).ExecuteAsync();
            }

            _logger.LogInformation("Cleared {Count} videos from playlist", itemIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error clearing playlist: {Error}", ex.Message);
        }
    }

    public async Task<int> AddVideosToPlaylist(string playlistId, List<string> videoIds)
    {
        int added = 0;

        foreach (var videoId in videoIds)
        {
            try
            {
                var playlistItem = new PlaylistItem
                {
                    Snippet = new PlaylistItemSnippet
                    {
                        PlaylistId = playlistId,
                        ResourceId = new ResourceId
                        {
                            Kind = "youtube#video",
                            VideoId = videoId
                        }
                    }
                };

                await _youtube.PlaylistItems.Insert(playlistItem, "snippet").ExecuteAsync();
                added++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not add video {VideoId}: {Error}", videoId, ex.Message);
            }
        }

        return added;
    }
}
