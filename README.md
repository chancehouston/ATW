# Adam The Woo - On This Day

A memorial project to celebrate Adam The Woo's legacy by sharing his daily vlogs "on this day" from years past.

## About

Adam The Woo was a beloved daily vlogger who shared his adventures, theme park explorations, and urban discoveries with the world for over a decade. This project honors his memory by automatically creating daily playlists and posts featuring videos he recorded on this day in previous years.

## How It Works

1. **One-time setup**: Fetch all videos from Adam's YouTube channel (@TheDailyWoo)
2. **Daily automation**: GitHub Actions runs every day to:
   - Find all videos recorded on today's date from past years
   - Create/update a YouTube playlist
   - Post to Reddit (r/Adamthewoo)
3. **Community driven**: Open source and accepting contributions

## Project Structure

```
adam-the-woo-on-this-day/
├── .github/
│   └── workflows/
│       └── daily_update.yml      # GitHub Actions workflow
├── src/
│   ├── fetch_videos.py           # One-time: fetch all channel videos
│   ├── update_playlist.py        # Daily: update today's playlist
│   ├── post_to_reddit.py         # Daily: post to Reddit
│   └── utils.py                  # Helper functions
├── data/
│   └── videos.json               # Video database (generated)
├── config/
│   └── config.yaml               # Configuration settings
├── .env.example                  # Example environment variables
├── .gitignore                    # Git ignore file
├── requirements.txt              # Python dependencies
└── README.md                     # This file
```

## Setup Instructions

### Prerequisites

- Python 3.9 or higher
- YouTube Data API credentials
- Reddit API credentials (for posting)
- GitHub account (for automation)

### Step 1: Clone the Repository

```bash
git clone https://github.com/yourusername/adam-the-woo-on-this-day.git
cd adam-the-woo-on-this-day
```

### Step 2: Install Dependencies

```bash
pip install -r requirements.txt
```

### Step 3: Configure API Credentials

1. Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and add your credentials:
   - **YouTube API Key**: Get from [Google Cloud Console](https://console.cloud.google.com/)
   - **Reddit API credentials**: Create an app at [Reddit Apps](https://www.reddit.com/prefs/apps)

3. **IMPORTANT**: Never commit `.env` to git! It's already in `.gitignore`.

### Step 4: Fetch Initial Video Data

Run this once to build the video database:

```bash
python src/fetch_videos.py
```

This will create `data/videos.json` with all of Adam's videos.

### Step 5: Test Locally

Test the playlist creation:

```bash
python src/update_playlist.py
```

Test Reddit posting:

```bash
python src/post_to_reddit.py
```

### Step 6: Set Up GitHub Actions

1. Fork this repository to your GitHub account
2. Go to Settings → Secrets and variables → Actions
3. Add the following secrets:
   - `YOUTUBE_API_KEY`
   - `YOUTUBE_CLIENT_ID`
   - `YOUTUBE_CLIENT_SECRET`
   - `YOUTUBE_REFRESH_TOKEN`
   - `REDDIT_CLIENT_ID`
   - `REDDIT_CLIENT_SECRET`
   - `REDDIT_USERNAME`
   - `REDDIT_PASSWORD`

The workflow will now run daily at 10:00 AM UTC.

## Configuration

Edit `config/config.yaml` to customize:

- Playlist title format
- Reddit post template
- Timezone settings
- Date parsing rules

## Contributing

This is a community memorial project. Contributions are welcome!

### Ways to Contribute

- **Fix video dates**: If you notice incorrect recording dates, submit a PR to `data/videos.json`
- **Improve date parsing**: Help improve automatic date detection
- **Add features**: Suggest or implement new features
- **Documentation**: Improve setup instructions

### Guidelines

- Be respectful - this is a memorial project
- Test your changes locally before submitting
- Follow the existing code style
- Update documentation as needed

## FAQ

**Q: Is this monetized?**  
A: No. This is 100% a free, open-source memorial project.

**Q: Who has access to the API keys?**  
A: Only the project maintainer. Keys are stored securely in GitHub Secrets.

**Q: What if a video's date is wrong?**  
A: Submit a PR to fix the date in `videos.json`, or open an issue.

**Q: Can I use this for another YouTuber?**  
A: Yes! Fork this project and modify the configuration.

**Q: What about video privacy?**  
A: We only use publicly available videos. If Adam's family requests removal, we'll honor that immediately.

## Acknowledgments

- Adam The Woo - for years of daily joy and adventure
- The r/Adamthewoo community - for their support and feedback
- All contributors to this project

## License

MIT License - See LICENSE file for details

## Contact

- Reddit: u/wiredcoder
- Issues: [GitHub Issues](https://github.com/yourusername/adam-the-woo-on-this-day/issues)

---

*"Every day's an adventure!"* - Adam The Woo
