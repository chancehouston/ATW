# Adam The Woo - On This Day Memorial Project (.NET 8)

A .NET 8 console application that celebrates Adam The Woo's legacy by automatically finding and sharing videos from "on this day" in history.

## Features

- **Fetch Videos**: Download all video metadata from Adam The Woo's YouTube channel
- **Update Playlist**: Automatically create/update YouTube playlists with videos from "on this day"
- **Post to Reddit**: Share daily "on this day" videos with the community

## Prerequisites

- .NET 8 SDK
- YouTube Data API key
- (Optional) YouTube OAuth credentials for playlist management
- (Optional) Reddit API credentials for posting

## Installation

1. Clone the repository
2. Install dependencies:
   ```bash
   dotnet restore
   ```

3. Create a `.env` file with your API credentials (see `.env.example`)

4. Build the project:
   ```bash
   dotnet build
   ```

## Configuration

Edit `config/config.yaml` to customize:
- Channel information
- Playlist title format
- Reddit post template
- Date parsing patterns
- Feature flags (dry run mode, etc.)

## Environment Variables

Create a `.env` file with the following variables:

```env
# YouTube Data API (required for fetching videos)
YOUTUBE_API_KEY=your_api_key_here

# YouTube OAuth (required for playlist management)
YOUTUBE_CLIENT_ID=your_client_id
YOUTUBE_CLIENT_SECRET=your_client_secret
YOUTUBE_REFRESH_TOKEN=your_refresh_token

# Reddit API (required for posting)
REDDIT_CLIENT_ID=your_reddit_client_id
REDDIT_CLIENT_SECRET=your_reddit_client_secret
REDDIT_USERNAME=your_reddit_username
REDDIT_PASSWORD=your_reddit_password
REDDIT_USER_AGENT=adam-the-woo-on-this-day-bot/1.0

# Optional overrides
YOUTUBE_CHANNEL_HANDLE=@TheDailyWoo
```

## Usage

### Fetch All Videos (First Time Setup)

Run this once to build the videos database:

```bash
dotnet run fetch-videos
```

This will:
- Connect to YouTube API
- Fetch all videos from the channel
- Parse dates from video titles
- Save to `data/videos.json`

### Update Today's Playlist

Run daily to create/update a playlist with today's "on this day" videos:

```bash
dotnet run update-playlist
```

This will:
- Find videos recorded on this date in previous years
- Create or update a YouTube playlist
- Clear old videos and add today's videos

### Post to Reddit

Run daily to share today's videos on Reddit:

```bash
dotnet run post-to-reddit [playlist_url]
```

Optional: Pass the playlist URL from the previous command.

This will:
- Find videos for today
- Format a Reddit post
- Post to the configured subreddit

### Dry Run Mode

To test without making actual changes, enable dry run mode in `config/config.yaml`:

```yaml
features:
  dry_run: true
```

## Project Structure

```
AdamTheWoo/
├── Models/
│   ├── AppConfig.cs          # Configuration models
│   └── Video.cs              # Video and database models
├── Services/
│   ├── YouTubeService.cs     # YouTube API integration
│   ├── PlaylistService.cs    # Playlist management
│   └── RedditService.cs      # Reddit API integration
├── Utilities/
│   ├── ConfigLoader.cs       # Configuration loading
│   ├── DatabaseManager.cs    # JSON database management
│   └── DateHelper.cs         # Date parsing and matching
├── config/
│   └── config.yaml           # Application configuration
├── data/
│   └── videos.json           # Videos database (generated)
├── Program.cs                # Main entry point
└── AdamTheWoo.csproj        # Project file
```

## Commands

```bash
dotnet run fetch-videos      # Fetch all videos from YouTube
dotnet run update-playlist   # Update today's playlist
dotnet run post-to-reddit    # Post to Reddit
dotnet run help             # Show help
```

## Porting Notes

This C# version is a complete port from the original Python implementation with the following improvements:

- **Type Safety**: Full type safety with C# type system
- **Async/Await**: Modern async patterns throughout
- **Dependency Injection**: Structured service architecture
- **Error Handling**: Comprehensive exception handling
- **Logging**: Integrated Microsoft.Extensions.Logging

### Key Differences from Python Version:

1. **Single Binary**: One executable with commands instead of separate scripts
2. **Strongly Typed Config**: YAML config mapped to C# classes
3. **NuGet Packages**: Uses official Google and Reddit NuGet packages
4. **Modern C# Patterns**: Uses latest C# 12 features and .NET 8

## Automated Scheduling

### GitHub Actions

Create `.github/workflows/daily-update.yml`:

```yaml
name: Daily Update

on:
  schedule:
    - cron: '0 10 * * *'  # Run daily at 10:00 UTC
  workflow_dispatch:

jobs:
  update:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Update Playlist
        env:
          YOUTUBE_API_KEY: ${{ secrets.YOUTUBE_API_KEY }}
          YOUTUBE_CLIENT_ID: ${{ secrets.YOUTUBE_CLIENT_ID }}
          YOUTUBE_CLIENT_SECRET: ${{ secrets.YOUTUBE_CLIENT_SECRET }}
          YOUTUBE_REFRESH_TOKEN: ${{ secrets.YOUTUBE_REFRESH_TOKEN }}
        run: dotnet run update-playlist

      - name: Post to Reddit
        env:
          REDDIT_CLIENT_ID: ${{ secrets.REDDIT_CLIENT_ID }}
          REDDIT_CLIENT_SECRET: ${{ secrets.REDDIT_CLIENT_SECRET }}
          REDDIT_USERNAME: ${{ secrets.REDDIT_USERNAME }}
          REDDIT_PASSWORD: ${{ secrets.REDDIT_PASSWORD }}
        run: dotnet run post-to-reddit
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is a memorial tribute to Adam The Woo and is not affiliated with or endorsed by Adam The Woo or his channels.

## Acknowledgments

- Adam The Woo for his amazing content and legacy
- The Adam The Woo community
- Original Python implementation authors
