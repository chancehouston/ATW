"""
Fetch all videos from Adam The Woo's YouTube channel
This script should be run once initially to build the videos database
"""

import os
import sys
from datetime import datetime
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError
import time

# Add src to path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from utils import (
    load_config,
    save_videos_db,
    load_videos_db,
    get_env_variable,
    setup_logging,
    parse_date_from_title,
    create_backup
)

def get_channel_id_from_handle(youtube, handle: str) -> str:
    """
    Get channel ID from channel handle (e.g., @TheDailyWoo)
    
    Args:
        youtube: YouTube API client
        handle: Channel handle (with or without @)
    
    Returns:
        Channel ID string
    """
    # Remove @ if present
    handle = handle.lstrip('@')
    
    try:
        # Search for the channel
        request = youtube.search().list(
            part='snippet',
            q=handle,
            type='channel',
            maxResults=5
        )
        response = request.execute()
        
        # Find exact match
        for item in response.get('items', []):
            custom_url = item['snippet'].get('customUrl', '').lower()
            if custom_url == f'@{handle.lower()}':
                return item['snippet']['channelId']
        
        # If no exact match, return first result
        if response.get('items'):
            return response['items'][0]['snippet']['channelId']
        
        raise Exception(f"Channel not found: {handle}")
        
    except HttpError as e:
        raise Exception(f"Error finding channel: {e}")

def get_channel_uploads_playlist(youtube, channel_id: str) -> tuple:
    """
    Get the uploads playlist ID for a channel
    
    Args:
        youtube: YouTube API client
        channel_id: Channel ID
    
    Returns:
        Tuple of (uploads_playlist_id, channel_title)
    """
    try:
        request = youtube.channels().list(
            part='contentDetails,snippet',
            id=channel_id
        )
        response = request.execute()
        
        if not response.get('items'):
            raise Exception(f"Channel not found: {channel_id}")
        
        channel = response['items'][0]
        uploads_playlist_id = channel['contentDetails']['relatedPlaylists']['uploads']
        channel_title = channel['snippet']['title']
        
        return uploads_playlist_id, channel_title
        
    except HttpError as e:
        raise Exception(f"Error getting channel info: {e}")

def fetch_all_videos_from_playlist(youtube, playlist_id: str, config: dict, logger) -> list:
    """
    Fetch all videos from a playlist (handles pagination)
    
    Args:
        youtube: YouTube API client
        playlist_id: Playlist ID (uploads playlist)
        config: Configuration dictionary
        logger: Logger instance
    
    Returns:
        List of video dictionaries
    """
    videos = []
    next_page_token = None
    page_count = 0
    
    logger.info(f"Fetching videos from playlist: {playlist_id}")
    
    while True:
        try:
            request = youtube.playlistItems().list(
                part='snippet,contentDetails',
                playlistId=playlist_id,
                maxResults=50,  # Max allowed by API
                pageToken=next_page_token
            )
            response = request.execute()
            
            page_count += 1
            items = response.get('items', [])
            logger.info(f"Page {page_count}: Found {len(items)} videos")
            
            for item in items:
                snippet = item['snippet']
                
                # Skip private/deleted videos
                if snippet['title'] == 'Private video' or snippet['title'] == 'Deleted video':
                    continue
                
                video_id = snippet['resourceId']['videoId']
                title = snippet['title']
                description = snippet.get('description', '')
                upload_date = snippet['publishedAt'][:10]  # YYYY-MM-DD
                
                # Try to parse recording date from title
                recording_date = None
                date_obj = parse_date_from_title(title, config)
                
                if date_obj:
                    recording_date = date_obj.strftime('%Y-%m-%d')
                elif config.get('date_parsing', {}).get('use_upload_date_fallback', True):
                    # Fallback to upload date
                    recording_date = upload_date
                
                video_data = {
                    'video_id': video_id,
                    'title': title,
                    'description': description,
                    'upload_date': upload_date,
                    'recording_date': recording_date,
                    'url': f'https://www.youtube.com/watch?v={video_id}',
                    'thumbnail': snippet['thumbnails'].get('high', {}).get('url', '')
                }
                
                videos.append(video_data)
            
            # Check if there are more pages
            next_page_token = response.get('nextPageToken')
            if not next_page_token:
                break
            
            # Be nice to the API
            time.sleep(0.5)
            
        except HttpError as e:
            logger.error(f"Error fetching videos: {e}")
            break
    
    logger.info(f"Total videos fetched: {len(videos)}")
    return videos

def main():
    """Main function to fetch all videos"""
    
    # Load configuration
    config = load_config()
    logger = setup_logging(config)
    
    logger.info("=" * 60)
    logger.info("Adam The Woo - On This Day: Video Fetch")
    logger.info("=" * 60)
    
    # Get API key
    api_key = get_env_variable('YOUTUBE_API_KEY')
    channel_handle = get_env_variable('YOUTUBE_CHANNEL_HANDLE', required=False) or config['channel']['handle']
    
    # Build YouTube API client
    logger.info("Connecting to YouTube API...")
    youtube = build('youtube', 'v3', developerKey=api_key)
    
    # Get channel ID
    logger.info(f"Finding channel: {channel_handle}")
    channel_id = get_channel_id_from_handle(youtube, channel_handle)
    logger.info(f"Channel ID: {channel_id}")
    
    # Get uploads playlist
    logger.info("Getting uploads playlist...")
    uploads_playlist_id, channel_title = get_channel_uploads_playlist(youtube, channel_id)
    logger.info(f"Channel: {channel_title}")
    logger.info(f"Uploads Playlist ID: {uploads_playlist_id}")
    
    # Fetch all videos
    videos = fetch_all_videos_from_playlist(youtube, uploads_playlist_id, config, logger)
    
    # Load existing database (if any) and backup
    db_path = os.path.join('data', 'videos.json')
    if os.path.exists(db_path):
        logger.info("Backing up existing database...")
        create_backup(db_path)
    
    # Create database structure
    videos_db = {
        'channel_info': {
            'channel_id': channel_id,
            'channel_name': channel_title,
            'channel_handle': channel_handle,
            'uploads_playlist_id': uploads_playlist_id,
            'total_videos': len(videos),
            'last_updated': datetime.now().isoformat()
        },
        'videos': videos
    }
    
    # Save to JSON
    logger.info("Saving videos database...")
    save_videos_db(videos_db)
    
    # Statistics
    logger.info("")
    logger.info("=" * 60)
    logger.info("FETCH COMPLETE")
    logger.info("=" * 60)
    logger.info(f"Total videos: {len(videos)}")
    
    videos_with_dates = sum(1 for v in videos if v.get('recording_date'))
    logger.info(f"Videos with recording dates: {videos_with_dates}")
    logger.info(f"Videos without dates: {len(videos) - videos_with_dates}")
    
    # Date range
    if videos_with_dates > 0:
        dates = [v['recording_date'] for v in videos if v.get('recording_date')]
        dates.sort()
        logger.info(f"Date range: {dates[0]} to {dates[-1]}")
    
    logger.info("")
    logger.info(f"Database saved to: {db_path}")
    logger.info("You can now run update_playlist.py to create today's playlist!")

if __name__ == '__main__':
    try:
        main()
    except Exception as e:
        print(f"ERROR: {e}")
        sys.exit(1)
