# Quick Start Guide

Get the Adam The Woo memorial project running in under 30 minutes!

## TL;DR

```bash
# 1. Clone and setup
git clone https://github.com/yourusername/adam-the-woo-on-this-day.git
cd adam-the-woo-on-this-day
pip install -r requirements.txt

# 2. Configure
cp .env.example .env
# Edit .env with your API keys

# 3. Fetch videos (one-time, ~5 minutes)
python src/fetch_videos.py

# 4. Test it
python src/update_playlist.py
python src/post_to_reddit.py
```

## What You Need

1. **YouTube API Key** - [Get it here](https://console.cloud.google.com/) (5 min)
2. **Reddit App Credentials** - [Create app here](https://www.reddit.com/prefs/apps) (2 min)
3. **Python 3.9+** - [Download](https://www.python.org/downloads/)

## Step-by-Step

### 1. Get YouTube API Key

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create new project
3. Enable "YouTube Data API v3"
4. Create credentials → API Key
5. Copy the key

### 2. Get Reddit Credentials

1. Go to [Reddit Apps](https://www.reddit.com/prefs/apps)
2. Create app (type: script)
3. Copy client ID and secret

### 3. Setup Project

```bash
# Install dependencies
pip install -r requirements.txt

# Configure
cp .env.example .env
nano .env  # Add your API keys
```

### 4. Fetch Adam's Videos

```bash
python src/fetch_videos.py
```

Wait ~5 minutes while it downloads all video data.

### 5. Test Locally

```bash
# Test playlist creation
python src/update_playlist.py

# Test Reddit post (dry run first!)
# Edit config/config.yaml and set: dry_run: true
python src/post_to_reddit.py
```

### 6. Deploy to GitHub

```bash
# Push to GitHub
git push origin main

# Add secrets in GitHub Settings → Secrets:
# - YOUTUBE_API_KEY
# - YOUTUBE_CLIENT_ID
# - YOUTUBE_CLIENT_SECRET  
# - YOUTUBE_REFRESH_TOKEN
# - REDDIT_CLIENT_ID
# - REDDIT_CLIENT_SECRET
# - REDDIT_USERNAME
# - REDDIT_PASSWORD
```

### 7. Done! 🎉

The workflow runs daily at 10 AM UTC automatically.

## Common Issues

**"No module named 'google'"**
→ Run: `pip install -r requirements.txt`

**"No videos in database"**
→ Run: `python src/fetch_videos.py` first

**YouTube OAuth error**
→ Run `update_playlist.py` locally to get refresh token

## Need More Help?

- 📖 Full guide: [SETUP_GUIDE.md](SETUP_GUIDE.md)
- 🤝 Contributing: [CONTRIBUTING.md](CONTRIBUTING.md)
- ❓ Issues: [GitHub Issues](https://github.com/yourusername/adam-the-woo-on-this-day/issues)

---

**Remember:** This is a memorial project - be respectful and have fun celebrating Adam's legacy! 🌟
