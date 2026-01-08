"""
Post daily "On This Day" content to Reddit
This script runs daily via GitHub Actions
"""

import os
import sys
import praw
from datetime import datetime

# Add src to path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from utils import (
    load_config,
    load_videos_db,
    get_videos_for_date,
    get_today_date,
    get_env_variable,
    setup_logging,
    format_video_list_for_reddit
)

def get_reddit_client(config: dict) -> praw.Reddit:
    """
    Create authenticated Reddit client
    
    Args:
        config: Configuration dictionary
    
    Returns:
        Authenticated Reddit instance
    """
    client_id = get_env_variable('REDDIT_CLIENT_ID')
    client_secret = get_env_variable('REDDIT_CLIENT_SECRET')
    username = get_env_variable('REDDIT_USERNAME')
    password = get_env_variable('REDDIT_PASSWORD')
    user_agent = get_env_variable('REDDIT_USER_AGENT', required=False) or 'adam-the-woo-on-this-day-bot/1.0'
    
    reddit = praw.Reddit(
        client_id=client_id,
        client_secret=client_secret,
        username=username,
        password=password,
        user_agent=user_agent
    )
    
    return reddit

def create_reddit_post(reddit: praw.Reddit, subreddit_name: str, title: str, 
                      body: str, flair_text: str = None, logger = None) -> str:
    """
    Create a post on Reddit
    
    Args:
        reddit: Authenticated Reddit instance
        subreddit_name: Name of subreddit
        title: Post title
        body: Post body (markdown)
        flair_text: Optional flair text
        logger: Logger instance
    
    Returns:
        URL of created post
    """
    try:
        subreddit = reddit.subreddit(subreddit_name)
        
        # Create post
        submission = subreddit.submit(
            title=title,
            selftext=body
        )
        
        # Add flair if specified and available
        if flair_text:
            try:
                # Get available flairs
                flair_choices = list(submission.flair.choices())
                
                # Find matching flair
                for flair in flair_choices:
                    if flair['flair_text'] == flair_text:
                        submission.flair.select(flair['flair_template_id'])
                        logger.info(f"Applied flair: {flair_text}")
                        break
            except Exception as e:
                logger.warning(f"Could not apply flair: {e}")
        
        post_url = f"https://www.reddit.com{submission.permalink}"
        logger.info(f"Post created: {post_url}")
        
        return post_url
        
    except Exception as e:
        raise Exception(f"Error creating Reddit post: {e}")

def main(playlist_url: str = None):
    """
    Main function to post to Reddit
    
    Args:
        playlist_url: Optional playlist URL to include in post
    """
    
    # Load configuration
    config = load_config()
    logger = setup_logging(config)
    
    # Check if dry run mode
    dry_run = config.get('features', {}).get('dry_run', False)
    
    logger.info("=" * 60)
    logger.info("Adam The Woo - On This Day: Reddit Post")
    if dry_run:
        logger.info("*** DRY RUN MODE - NO POST WILL BE MADE ***")
    logger.info("=" * 60)
    
    # Check if Reddit posting is enabled
    if not config.get('features', {}).get('post_to_reddit', True):
        logger.info("Reddit posting disabled in config")
        return
    
    # Load videos database
    logger.info("Loading videos database...")
    videos_db = load_videos_db()
    
    if not videos_db.get('videos'):
        logger.error("No videos in database. Run fetch_videos.py first!")
        return
    
    # Get today's date
    timezone = config.get('date_parsing', {}).get('timezone', 'America/New_York')
    month, day, formatted_date = get_today_date(timezone)
    logger.info(f"Today's date: {formatted_date}")
    
    # Find videos for today
    today_videos = get_videos_for_date(videos_db, month, day)
    logger.info(f"Found {len(today_videos)} videos for this date")
    
    if not today_videos:
        logger.info("No videos found for today. No post will be made.")
        return
    
    # Format post content
    reddit_config = config.get('reddit', {})
    
    # Post title
    post_title = reddit_config.get('post_title_format', 'On This Day - {month} {day}').format(
        month=formatted_date.split()[0],
        day=formatted_date.split()[1],
        date=formatted_date
    )
    
    # Format video list
    video_list = format_video_list_for_reddit(today_videos)
    
    # Post body
    post_template = reddit_config.get('post_template', '')
    post_body = post_template.format(
        month=formatted_date.split()[0],
        day=formatted_date.split()[1],
        date=formatted_date,
        video_list=video_list,
        playlist_url=playlist_url or 'Coming soon!'
    )
    
    # Preview
    logger.info("")
    logger.info("Post preview:")
    logger.info("-" * 60)
    logger.info(f"Title: {post_title}")
    logger.info("")
    logger.info(post_body)
    logger.info("-" * 60)
    logger.info("")
    
    if dry_run:
        logger.info("Dry run mode - post not submitted")
        return
    
    # Authenticate with Reddit
    logger.info("Authenticating with Reddit...")
    reddit = get_reddit_client(config)
    logger.info(f"Authenticated as: u/{reddit.user.me().name}")
    
    # Post to subreddit
    subreddit_name = reddit_config.get('subreddit', 'Adamthewoo')
    flair_text = reddit_config.get('flair_text')
    
    logger.info(f"Posting to r/{subreddit_name}...")
    post_url = create_reddit_post(
        reddit=reddit,
        subreddit_name=subreddit_name,
        title=post_title,
        body=post_body,
        flair_text=flair_text,
        logger=logger
    )
    
    # Results
    logger.info("")
    logger.info("=" * 60)
    logger.info("POST COMPLETE")
    logger.info("=" * 60)
    logger.info(f"Subreddit: r/{subreddit_name}")
    logger.info(f"Post URL: {post_url}")
    logger.info("")

if __name__ == '__main__':
    try:
        # Can optionally pass playlist URL as command line argument
        playlist_url = sys.argv[1] if len(sys.argv) > 1 else None
        main(playlist_url)
    except Exception as e:
        print(f"ERROR: {e}")
        sys.exit(1)
