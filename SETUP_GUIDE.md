# Setup Guide - Adam The Woo - On This Day

This guide will walk you through setting up the project from scratch.

## Prerequisites

- Python 3.9 or higher
- Git
- A Google account (for YouTube API)
- A Reddit account (for posting)
- A GitHub account (for automation)

## Part 1: Local Setup

### Step 1: Clone the Repository

```bash
git clone https://github.com/yourusername/adam-the-woo-on-this-day.git
cd adam-the-woo-on-this-day
```

### Step 2: Install Python Dependencies

```bash
# Create virtual environment (recommended)
python -m venv venv

# Activate virtual environment
# On Windows:
venv\Scripts\activate
# On Mac/Linux:
source venv/bin/activate

# Install dependencies
pip install -r requirements.txt
```

### Step 3: Get YouTube API Credentials

#### 3a. Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click "Select a project" → "New Project"
3. Name it "Adam The Woo Memorial" (or similar)
4. Click "Create"

#### 3b. Enable YouTube Data API

1. In your project, go to "APIs & Services" → "Library"
2. Search for "YouTube Data API v3"
3. Click on it and press "Enable"

#### 3c. Create API Key (for reading data)

1. Go to "APIs & Services" → "Credentials"
2. Click "Create Credentials" → "API Key"
3. Copy the API key (you'll add this to .env)
4. Optional: Click "Restrict Key" to limit it to YouTube Data API v3

#### 3d. Create OAuth Credentials (for creating playlists)

1. Still in "Credentials", click "Create Credentials" → "OAuth client ID"
2. If prompted, configure OAuth consent screen:
   - User Type: External
   - App name: "Adam The Woo Memorial"
   - User support email: Your email
   - Developer contact: Your email
   - Scopes: Don't add any (we'll use default)
   - Test users: Add your email
3. Back to "Create OAuth client ID":
   - Application type: Desktop app
   - Name: "Adam The Woo Memorial Client"
4. Click "Create"
5. Download the JSON file or copy Client ID and Client Secret

### Step 4: Get Reddit API Credentials

#### 4a. Create Reddit App

1. Go to [Reddit Apps](https://www.reddit.com/prefs/apps)
2. Scroll to "Developed Applications"
3. Click "Create App" or "Create Another App"
4. Fill in:
   - Name: "Adam The Woo On This Day"
   - App type: Select "script"
   - Description: "Memorial bot for daily posts"
   - About url: (leave blank or add GitHub URL later)
   - Redirect uri: `http://localhost:8080`
5. Click "Create app"
6. Note the following:
   - Client ID: Under the app name (looks like: abc123def456)
   - Client Secret: The "secret" field

### Step 5: Configure Environment Variables

```bash
# Copy the example file
cp .env.example .env

# Edit .env with your favorite text editor
nano .env  # or: vim .env, code .env, etc.
```

Fill in your credentials:

```ini
# YouTube API Configuration
YOUTUBE_API_KEY=YOUR_API_KEY_HERE
YOUTUBE_CLIENT_ID=YOUR_CLIENT_ID_HERE
YOUTUBE_CLIENT_SECRET=YOUR_CLIENT_SECRET_HERE
YOUTUBE_REFRESH_TOKEN=  # Leave blank for now

# Reddit API Configuration
REDDIT_CLIENT_ID=YOUR_REDDIT_CLIENT_ID_HERE
REDDIT_CLIENT_SECRET=YOUR_REDDIT_SECRET_HERE
REDDIT_USERNAME=wiredcoder
REDDIT_PASSWORD=YOUR_REDDIT_PASSWORD_HERE
REDDIT_USER_AGENT=adam-the-woo-on-this-day-bot/1.0

# Channel Configuration
YOUTUBE_CHANNEL_HANDLE=@TheDailyWoo

# Timezone
TIMEZONE=America/New_York
```

**Important:** Never commit the `.env` file! It's already in `.gitignore`.

## Part 2: Initial Data Collection

### Step 6: Fetch All Videos

This step will take a few minutes and will create `data/videos.json`:

```bash
python src/fetch_videos.py
```

You should see output like:
```
Connecting to YouTube API...
Finding channel: @TheDailyWoo
Channel ID: UCxxxxxxxxxx
Getting uploads playlist...
Fetching videos from playlist...
Page 1: Found 50 videos
Page 2: Found 50 videos
...
Total videos fetched: 5432
Videos with recording dates: 5120
Database saved to: data/videos.json
```

**Note:** The `videos.json` file should NOT be committed to GitHub if it's very large. Consider:
- Using Git LFS (Large File Storage)
- Storing it as a GitHub Release asset
- Hosting it separately and downloading it in the workflow

### Step 7: Get YouTube Refresh Token

To create/update playlists, you need OAuth authentication:

```bash
python src/update_playlist.py
```

On first run, this will:
1. Open a browser window
2. Ask you to log in to Google
3. Ask for permission to manage YouTube
4. Redirect to localhost (may show error - that's OK!)
5. Display a refresh token

**Copy the YOUTUBE_REFRESH_TOKEN** and add it to your `.env` file.

### Step 8: Test Locally

#### Test Playlist Creation

```bash
python src/update_playlist.py
```

This should:
- Find videos for today's date
- Create or update a YouTube playlist
- Show you the playlist URL

#### Test Reddit Posting

First, enable dry run mode in `config/config.yaml`:

```yaml
features:
  dry_run: true
```

Then run:

```bash
python src/post_to_reddit.py "https://youtube.com/playlist?list=YOUR_PLAYLIST_ID"
```

This will show what the Reddit post would look like without actually posting.

When ready to test for real, set `dry_run: false` and run again.

## Part 3: GitHub Actions Setup

### Step 9: Push to GitHub

If you forked the repo:
```bash
git add .
git commit -m "Initial setup with my configuration"
git push origin main
```

If creating a new repo:
```bash
git init
git add .
git commit -m "Initial commit"
git remote add origin https://github.com/yourusername/adam-the-woo-on-this-day.git
git push -u origin main
```

### Step 10: Add GitHub Secrets

1. Go to your GitHub repository
2. Click "Settings" → "Secrets and variables" → "Actions"
3. Click "New repository secret" for each of these:

| Secret Name | Value |
|-------------|-------|
| `YOUTUBE_API_KEY` | Your YouTube API key |
| `YOUTUBE_CLIENT_ID` | Your YouTube OAuth client ID |
| `YOUTUBE_CLIENT_SECRET` | Your YouTube OAuth client secret |
| `YOUTUBE_REFRESH_TOKEN` | Your YouTube refresh token |
| `REDDIT_CLIENT_ID` | Your Reddit app client ID |
| `REDDIT_CLIENT_SECRET` | Your Reddit app secret |
| `REDDIT_USERNAME` | Your Reddit username (e.g., wiredcoder) |
| `REDDIT_PASSWORD` | Your Reddit password |

### Step 11: Upload Videos Database

Since `videos.json` is in `.gitignore`, you need to make it available to GitHub Actions.

**Option A: Use Git LFS** (if file < 100MB)
```bash
git lfs track "data/videos.json"
git add .gitattributes data/videos.json
git commit -m "Add videos database with Git LFS"
git push
```

**Option B: Create a Release**
1. Go to your GitHub repo
2. Click "Releases" → "Create a new release"
3. Tag version: v1.0.0
4. Title: "Initial Release"
5. Upload `data/videos.json` as an asset
6. Publish release
7. Update `.github/workflows/daily_update.yml` to download it

**Option C: Store Separately**
- Host on Google Drive / Dropbox with public link
- Download in workflow before running scripts

### Step 12: Test GitHub Actions

1. Go to "Actions" tab in your GitHub repo
2. Click "Daily Update - On This Day"
3. Click "Run workflow" → "Run workflow"
4. Watch it run!

If it succeeds:
- ✅ Check your YouTube for the new playlist
- ✅ Check r/Adamthewoo for the new post

## Part 4: Maintenance

### Daily Monitoring

The workflow runs automatically at 10:00 AM UTC daily. Check:
- GitHub Actions logs (if it fails)
- YouTube playlist updates
- Reddit posts

### Updating the Database

As new videos are uploaded (if any), re-run:
```bash
python src/fetch_videos.py
```

Then upload the updated `videos.json` using your chosen method.

### Fixing Dates

If you notice incorrect dates:
1. Edit `data/videos.json` directly
2. Find the video
3. Update `recording_date` field
4. Save and re-upload

Or accept community pull requests with fixes!

## Troubleshooting

### "ModuleNotFoundError"
- Make sure you installed requirements: `pip install -r requirements.txt`
- Make sure virtual environment is activated

### "No videos in database"
- Run `fetch_videos.py` first
- Check that `data/videos.json` exists

### YouTube API Quota Exceeded
- YouTube API has daily quotas (10,000 units/day)
- Fetching videos uses ~1 unit per 50 videos
- Wait 24 hours and try again

### OAuth Error
- Make sure OAuth consent screen is configured
- Add yourself as test user
- Re-authenticate: delete `token.pickle` and run `update_playlist.py`

### Reddit Post Failed
- Check username/password in secrets
- Verify Reddit app credentials
- Check if subreddit allows bots
- Some subreddits have karma requirements

### GitHub Actions Fails
- Check logs in Actions tab
- Verify all secrets are set correctly
- Make sure `videos.json` is accessible

## Need Help?

- Open an issue on GitHub
- Check existing issues for solutions
- Read CONTRIBUTING.md for guidelines

## Next Steps

Once everything is working:
1. Monitor the first few daily posts
2. Invite community contributions
3. Consider additional features (Discord, Twitter, etc.)
4. Enjoy celebrating Adam's legacy! 🎉
