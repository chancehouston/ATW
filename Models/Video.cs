using System.Text.Json.Serialization;

namespace AdamTheWoo.Models;

public class Video
{
    [JsonPropertyName("video_id")]
    public string VideoId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("upload_date")]
    public string UploadDate { get; set; } = string.Empty;

    [JsonPropertyName("recording_date")]
    public string? RecordingDate { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; } = string.Empty;
}

public class ChannelInfo
{
    [JsonPropertyName("channel_id")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channel_name")]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("channel_handle")]
    public string ChannelHandle { get; set; } = string.Empty;

    [JsonPropertyName("uploads_playlist_id")]
    public string UploadsPlaylistId { get; set; } = string.Empty;

    [JsonPropertyName("total_videos")]
    public int TotalVideos { get; set; }

    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; set; } = string.Empty;
}

public class VideosDatabase
{
    [JsonPropertyName("channel_info")]
    public ChannelInfo ChannelInfo { get; set; } = new();

    [JsonPropertyName("videos")]
    public List<Video> Videos { get; set; } = new();
}
