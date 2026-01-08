# Contributing to Adam The Woo - On This Day

Thank you for your interest in contributing to this memorial project! This guide will help you get started.

## Ways to Contribute

### 1. Fix Video Dates

If you notice a video has an incorrect recording date:

1. Fork the repository
2. Edit `data/videos.json`
3. Find the video entry and update the `recording_date` field (format: YYYY-MM-DD)
4. Submit a pull request with a clear description of the change

**Example:**
```json
{
  "video_id": "abc123",
  "title": "Exploring Disneyland - January 15, 2015",
  "recording_date": "2015-01-15"  // ← Fix this date
}
```

### 2. Improve Date Parsing

Help improve automatic date detection:

1. Add new date format patterns to `config/config.yaml`
2. Improve the `parse_date_from_title()` function in `src/utils.py`
3. Test with actual video titles
4. Submit a pull request

### 3. Add Features

Ideas for new features:
- Post to Twitter/X
- Discord bot integration
- Web dashboard to browse videos
- Improved date detection using AI
- Video categorization/tagging

**Before starting:**
1. Open an issue to discuss your idea
2. Wait for feedback from maintainers
3. Fork and create a feature branch
4. Submit a pull request when ready

### 4. Improve Documentation

- Fix typos or unclear instructions
- Add troubleshooting tips
- Improve setup instructions
- Add examples

### 5. Report Issues

Found a bug? Have a suggestion?

1. Check existing issues first
2. Open a new issue with:
   - Clear description
   - Steps to reproduce (for bugs)
   - Expected vs actual behavior
   - Your environment (OS, Python version, etc.)

## Code Guidelines

### Python Style

- Follow PEP 8 style guide
- Use meaningful variable names
- Add docstrings to functions
- Keep functions focused and small
- Add comments for complex logic

### Testing

Before submitting:
1. Test your changes locally
2. Verify no existing functionality breaks
3. Test with different scenarios
4. Check for API errors

### Commit Messages

Write clear commit messages:
- ✅ "Fix: Correct date parsing for MM/DD/YYYY format"
- ✅ "Feature: Add Discord integration"
- ❌ "update stuff"
- ❌ "fix bug"

## Pull Request Process

1. **Fork** the repository
2. **Create a branch** from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Make your changes**
4. **Test thoroughly**
5. **Commit** with clear messages
6. **Push** to your fork
7. **Open a pull request** with:
   - Clear title
   - Description of changes
   - Why the change is needed
   - How you tested it

## Code of Conduct

### Our Pledge

This is a memorial project honoring Adam The Woo. We are committed to making participation respectful and harassment-free.

### Expected Behavior

- Be respectful and kind
- Welcome newcomers
- Accept constructive criticism
- Focus on what's best for the project
- Show empathy

### Unacceptable Behavior

- Harassment or discrimination
- Trolling or insulting comments
- Disrespectful behavior
- Any conduct inappropriate for a memorial project

## Getting Help

- **Questions?** Open an issue with the "question" label
- **Stuck?** Ask in the issue or pull request
- **Not sure?** Ask first before starting work

## Recognition

Contributors will be recognized in:
- README.md contributors section
- GitHub contributors page
- Release notes

## API Keys and Secrets

**NEVER commit API keys or secrets!**

- Use `.env` for local development
- `.env` is in `.gitignore`
- GitHub Secrets for CI/CD
- If you accidentally commit a secret:
  1. Revoke it immediately
  2. Generate a new one
  3. Notify maintainers

## Testing Locally

### Initial Setup
```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/adam-the-woo-on-this-day.git
cd adam-the-woo-on-this-day

# Install dependencies
pip install -r requirements.txt

# Copy and configure environment
cp .env.example .env
# Edit .env with your API keys
```

### Testing Changes
```bash
# Test video fetching
python src/fetch_videos.py

# Test playlist update
python src/update_playlist.py

# Test Reddit posting
python src/post_to_reddit.py
```

### Dry Run Mode

Enable dry run in `config/config.yaml` to test without making actual API calls:
```yaml
features:
  dry_run: true
```

## Questions?

Feel free to reach out:
- Open an issue
- Comment on existing issues/PRs
- Contact maintainers

Thank you for helping honor Adam's legacy! 🎉
