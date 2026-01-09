namespace AdamTheWoo.Models;

public class AppConfig
{
    public ChannelConfig Channel { get; set; } = new();
    public PlaylistConfig Playlist { get; set; } = new();
    public RedditConfig Reddit { get; set; } = new();
    public DateParsingConfig DateParsing { get; set; } = new();
    public ScheduleConfig Schedule { get; set; } = new();
    public FeaturesConfig Features { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
}

public class ChannelConfig
{
    public string Handle { get; set; } = "@TheDailyWoo";
    public string Name { get; set; } = "Adam The Woo";
}

public class PlaylistConfig
{
    public string TitleFormat { get; set; } = "Adam The Woo - On This Day: {month} {day}";
    public string DescriptionFormat { get; set; } = "";
    public int MaxVideos { get; set; } = 200;
}

public class RedditConfig
{
    public string Subreddit { get; set; } = "Adamthewoo";
    public string PostTitleFormat { get; set; } = "On This Day - {month} {day} - Adam The Woo's Adventures";
    public string PostTemplate { get; set; } = "";
    public string? FlairText { get; set; }
}

public class DateParsingConfig
{
    public List<string> TitlePatterns { get; set; } = new();
    public bool UseUploadDateFallback { get; set; } = true;
    public string Timezone { get; set; } = "America/New_York";
}

public class ScheduleConfig
{
    public int Hour { get; set; } = 10;
    public int Minute { get; set; } = 0;
    public string Timezone { get; set; } = "UTC";
}

public class FeaturesConfig
{
    public bool CreatePlaylist { get; set; } = true;
    public bool PostToReddit { get; set; } = true;
    public bool PostToTwitter { get; set; } = false;
    public bool DryRun { get; set; } = false;
}

public class LoggingConfig
{
    public string Level { get; set; } = "INFO";
    public string File { get; set; } = "adam_the_woo_memorial.log";
}
