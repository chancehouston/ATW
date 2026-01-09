using System.Globalization;
using AdamTheWoo.Models;

namespace AdamTheWoo.Utilities;

public static class DateHelper
{
    public static DateTime? ParseDateFromTitle(string title, AppConfig config)
    {
        // Try each configured pattern
        foreach (var pattern in config.DateParsing.TitlePatterns)
        {
            if (DateTime.TryParseExact(title, pattern, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        // Try fuzzy parsing as fallback
        if (DateTime.TryParse(title, out var fuzzyDate))
        {
            return fuzzyDate;
        }

        return null;
    }

    public static List<Video> GetVideosForDate(VideosDatabase database, int month, int day)
    {
        var matchingVideos = new List<Video>();

        foreach (var video in database.Videos)
        {
            if (string.IsNullOrEmpty(video.RecordingDate))
                continue;

            if (DateTime.TryParse(video.RecordingDate, out var recordingDate))
            {
                if (recordingDate.Month == month && recordingDate.Day == day)
                {
                    matchingVideos.Add(video);
                }
            }
        }

        // Sort by year (oldest first)
        matchingVideos.Sort((a, b) =>
            string.Compare(a.RecordingDate, b.RecordingDate, StringComparison.Ordinal));

        return matchingVideos;
    }

    public static (int Month, int Day, string FormattedDate) GetTodayDate(string? timezoneId = null)
    {
        DateTime now;

        if (!string.IsNullOrEmpty(timezoneId))
        {
            try
            {
                var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
                now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
            }
            catch
            {
                now = DateTime.Now;
            }
        }
        else
        {
            now = DateTime.Now;
        }

        var formattedDate = now.ToString("MMMM dd");
        return (now.Month, now.Day, formattedDate);
    }

    public static string FormatVideoListForReddit(List<Video> videos)
    {
        if (videos.Count == 0)
        {
            return "*No videos found for this date.*";
        }

        var lines = new List<string>();
        foreach (var video in videos)
        {
            var year = "Unknown";
            if (!string.IsNullOrEmpty(video.RecordingDate) &&
                DateTime.TryParse(video.RecordingDate, out var date))
            {
                year = date.Year.ToString();
            }

            lines.Add($"**{year}:** [{video.Title}]({video.Url})");
        }

        return string.Join("\n\n", lines);
    }
}
