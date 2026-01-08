"""
Update YouTube playlist with videos from "on this day"
This script runs daily via GitHub Actions
"""

import os
import sys
from datetime import datetime
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError
from google.oauth2.credentials import Credentials
from google_auth_oauthlib.flow import InstalledAppFlow
from google.auth.transport.requests import Request
import pickle

# Add src to path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from utils import (
    load_config,
    load_videos_db,
    get_videos_for_date,
    get_today_date,
    get_env_variable,
    setup_logging
)

# OAuth scopes needed for playlist management
SCOPES = ['https://www.googleapis.com/auth/youtube.force-ssl']

def get_authenticated_service():
    """
    Get authenticated YouTube service for playlist management
    Uses OAuth2 credentials from environment or token file
    """
    credentials = None
    token_file = 'token.pickle'
    
    # Check for environment variables (for GitHub Actions)
    client_id = os.getenv('YOUTUBE_CLIENT_ID')
    client_secret = os.getenv('YOUTUBE_CLIENT_SECRET')
    refresh_token = os.getenv('YOUTUBE_REFRESH_TOKEN')
    
    if client_id and client_secret and refresh_token:
        # Use credentials from environment
        credentials = Credentials(
            token=None,
            refresh_token=refresh_token,
            token_uri='https://oauth2.googleapis.com/token',
            client_id=client_id,
            client_secret=client_secret,
            scopes=SCOPES
        )
    elif os.path.exists(token_file):
        # Load from pickle file (local development)
        with open(token_file, 'rb') as token:
            credentials = pickle.load(token)
    
    # Refresh if expired
    if credentials and credentials.expired and credentials.refresh_token:
        credentials.refresh(Request())
    
    # If no valid credentials, need to authenticate
    if not credentials or not credentials.valid:
        if client_id and client_secret:
            # Manual OAuth flow for first-time setup
            flow = InstalledAppFlow.from_client_config(
                {
                    "installed": {
                        "client_id": client_id,
                        "client_secret": client_secret,
                        "auth_uri": "https://accounts.google.com/o/oauth2/auth",
                        "token_uri": "https://oauth2.googleapis.com/token",
                        "redirect_uris": ["http://localhost"]
                    }
                },
                SCOPES
            )
            credentials = flow.run_local_server(port=0)
            
            # Save credentials for next run
            with open(token_file, 'wb') as token:
                pickle.dump(credentials, token)
            
            print("\n" + "=" * 60)
            print("IMPORTANT: Save these for GitHub Secrets:")
            print("=" * 60)
            print(f"YOUTUBE_REFRESH_TOKEN={credentials.refresh_token}")
            print("=" * 60 + "\n")
        else:
            raise Exception("No YouTube credentials available. Please set environment variables.")
    
    return build('youtube', 'v3', credentials=credentials)

def find_or_create_playlist(youtube, title: str, description: str, logger) -> str:
    """
    Find existing playlist by title or create new one
    
    Args:
        youtube: Authenticated YouTube API client
        title: Playlist title
        description: Playlist description
        logger: Logger instance
    
    Returns:
        Playlist ID
    """
    try:
        # Search for existing playlist
        request = youtube.playlists().list(
            part='snippet',
            mine=True,
            maxResults=50
        )
        response = request.execute()
        
        for item in response.get('items', []):
            if item['snippet']['title'] == title:
                playlist_id = item['id']
                logger.info(f"Found existing playlist: {playlist_id}")
                return playlist_id
        
        # Create new playlist if not found
        logger.info(f"Creating new playlist: {title}")
        request = youtube.playlists().insert(
            part='snippet,status',
            body={
                'snippet': {
                    'title': title,
                    'description': description
                },
                'status': {
                    'privacyStatus': 'public'
                }
            }
        )
        response = request.execute()
        playlist_id = response['id']
        logger.info(f"Created playlist: {playlist_id}")
        return playlist_id
        
    except HttpError as e:
        raise Exception(f"Error managing playlist: {e}")

def clear_playlist(youtube, playlist_id: str, logger) -> None:
    """
    Remove all videos from a playlist
    
    Args:
        youtube: Authenticated YouTube API client
        playlist_id: Playlist ID to clear
        logger: Logger instance
    """
    try:
        # Get all items in playlist
        next_page_token = None
        item_ids = []
        
        while True:
            request = youtube.playlistItems().list(
                part='id',
                playlistId=playlist_id,
                maxResults=50,
                pageToken=next_page_token
            )
            response = request.execute()
            
            for item in response.get('items', []):
                item_ids.append(item['id'])
            
            next_page_token = response.get('nextPageToken')
            if not next_page_token:
                break
        
        # Delete each item
        for item_id in item_ids:
            youtube.playlistItems().delete(id=item_id).execute()
        
        logger.info(f"Cleared {len(item_ids)} videos from playlist")
        
    except HttpError as e:
        logger.error(f"Error clearing playlist: {e}")

def add_videos_to_playlist(youtube, playlist_id: str, video_ids: list, logger) -> int:
    """
    Add videos to playlist
    
    Args:
        youtube: Authenticated YouTube API client
        playlist_id: Playlist ID
        video_ids: List of video IDs to add
        logger: Logger instance
    
    Returns:
        Number of videos successfully added
    """
    added = 0
    
    for video_id in video_ids:
        try:
            request = youtube.playlistItems().insert(
                part='snippet',
                body={
                    'snippet': {
                        'playlistId': playlist_id,
                        'resourceId': {
                            'kind': 'youtube#video',
                            'videoId': video_id
                        }
                    }
                }
            )
            request.execute()
            added += 1
            
        except HttpError as e:
            logger.warning(f"Could not add video {video_id}: {e}")
    
    return added

def main():
    """Main function to update today's playlist"""
    
    # Load configuration
    config = load_config()
    logger = setup_logging(config)
    
    # Check if dry run mode
    dry_run = config.get('features', {}).get('dry_run', False)
    
    logger.info("=" * 60)
    logger.info("Adam The Woo - On This Day: Playlist Update")
    if dry_run:
        logger.info("*** DRY RUN MODE - NO CHANGES WILL BE MADE ***")
    logger.info("=" * 60)
    
    # Load videos database
    logger.info("Loading videos database...")
    videos_db = load_videos_db()
    
    if not videos_db.get('videos'):
        logger.error("No videos in database. Run fetch_videos.py first!")
        return
    
    # Get today's date
    timezone = config.get('date_parsing', {}).get('timezone', 'America/New_York')
    month, day, formatted_date = get_today_date(timezone)
    logger.info(f"Today's date: {formatted_date} (Month: {month}, Day: {day})")
    
    # Find videos for today
    today_videos = get_videos_for_date(videos_db, month, day)
    logger.info(f"Found {len(today_videos)} videos for this date")
    
    if not today_videos:
        logger.info("No videos found for today. Exiting.")
        return
    
    # Display videos
    for video in today_videos:
        year = video.get('recording_date', '')[:4]
        logger.info(f"  - {year}: {video.get('title')}")
    
    if dry_run:
        logger.info("Dry run mode - would have updated playlist with these videos")
        return
    
    # Check if playlist creation is enabled
    if not config.get('features', {}).get('create_playlist', True):
        logger.info("Playlist creation disabled in config")
        return
    
    # Authenticate and create/update playlist
    logger.info("Authenticating with YouTube...")
    youtube = get_authenticated_service()
    
    # Format playlist title and description
    playlist_config = config.get('playlist', {})
    playlist_title = playlist_config.get('title_format', 'On This Day: {month} {day}').format(
        month=formatted_date.split()[0],
        day=formatted_date.split()[1]
    )
    
    playlist_description = playlist_config.get('description_format', '').format(
        month=formatted_date.split()[0],
        day=formatted_date.split()[1],
        date=formatted_date
    )
    
    # Find or create playlist
    logger.info(f"Managing playlist: {playlist_title}")
    playlist_id = find_or_create_playlist(youtube, playlist_title, playlist_description, logger)
    
    # Clear existing videos
    logger.info("Clearing existing playlist items...")
    clear_playlist(youtube, playlist_id, logger)
    
    # Add today's videos
    logger.info("Adding videos to playlist...")
    video_ids = [v['video_id'] for v in today_videos]
    added = add_videos_to_playlist(youtube, playlist_id, video_ids, logger)
    
    # Results
    playlist_url = f"https://www.youtube.com/playlist?list={playlist_id}"
    
    logger.info("")
    logger.info("=" * 60)
    logger.info("UPDATE COMPLETE")
    logger.info("=" * 60)
    logger.info(f"Playlist: {playlist_title}")
    logger.info(f"Videos added: {added}/{len(today_videos)}")
    logger.info(f"Playlist URL: {playlist_url}")
    logger.info("")
    
    # Return playlist URL for use by other scripts
    return playlist_url

if __name__ == '__main__':
    try:
        main()
    except Exception as e:
        print(f"ERROR: {e}")
        sys.exit(1)
