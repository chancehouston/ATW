"""
Utility functions for Adam The Woo - On This Day memorial project
"""

import json
import os
import logging
from datetime import datetime
from typing import Dict, List, Optional
import yaml
from dotenv import load_dotenv
import pytz

# Load environment variables
load_dotenv()

def setup_logging(config: Dict) -> logging.Logger:
    """Set up logging configuration"""
    log_level = getattr(logging, config.get('logging', {}).get('level', 'INFO'))
    log_file = config.get('logging', {}).get('file', 'adam_the_woo_memorial.log')
    
    logging.basicConfig(
        level=log_level,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler(log_file),
            logging.StreamHandler()
        ]
    )
    
    return logging.getLogger('adam_the_woo_memorial')

def load_config() -> Dict:
    """Load configuration from config.yaml"""
    config_path = os.path.join('config', 'config.yaml')
    
    try:
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
        return config
    except FileNotFoundError:
        raise Exception(f"Configuration file not found: {config_path}")
    except yaml.YAMLError as e:
        raise Exception(f"Error parsing configuration file: {e}")

def load_videos_db() -> Dict:
    """Load videos database from JSON file"""
    db_path = os.path.join('data', 'videos.json')
    
    if not os.path.exists(db_path):
        # Return empty structure if database doesn't exist yet
        return {
            "channel_info": {
                "channel_id": "",
                "channel_name": "Adam The Woo",
                "channel_handle": "@TheDailyWoo",
                "last_updated": None
            },
            "videos": []
        }
    
    try:
        with open(db_path, 'r', encoding='utf-8') as f:
            return json.load(f)
    except json.JSONDecodeError as e:
        raise Exception(f"Error parsing videos database: {e}")

def save_videos_db(data: Dict) -> None:
    """Save videos database to JSON file"""
    db_path = os.path.join('data', 'videos.json')
    
    # Create data directory if it doesn't exist
    os.makedirs('data', exist_ok=True)
    
    # Update last_updated timestamp
    data['channel_info']['last_updated'] = datetime.now().isoformat()
    
    try:
        with open(db_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
    except Exception as e:
        raise Exception(f"Error saving videos database: {e}")

def parse_date_from_title(title: str, config: Dict) -> Optional[datetime]:
    """
    Try to extract a date from video title using configured patterns
    
    Args:
        title: Video title
        config: Configuration dictionary with date_parsing patterns
    
    Returns:
        datetime object if date found, None otherwise
    """
    from dateutil import parser
    
    patterns = config.get('date_parsing', {}).get('title_patterns', [])
    
    # Try each pattern
    for pattern in patterns:
        try:
            # Try to find a date string matching the pattern
            date_obj = datetime.strptime(title, pattern)
            return date_obj
        except ValueError:
            continue
    
    # Try dateutil parser as fallback (more flexible)
    try:
        date_obj = parser.parse(title, fuzzy=True)
        return date_obj
    except:
        pass
    
    return None

def get_videos_for_date(videos_db: Dict, month: int, day: int) -> List[Dict]:
    """
    Get all videos recorded on a specific month/day across all years
    
    Args:
        videos_db: Videos database dictionary
        month: Month (1-12)
        day: Day (1-31)
    
    Returns:
        List of video dictionaries matching the date
    """
    matching_videos = []
    
    for video in videos_db.get('videos', []):
        recording_date = video.get('recording_date')
        
        if recording_date:
            try:
                date_obj = datetime.fromisoformat(recording_date)
                if date_obj.month == month and date_obj.day == day:
                    matching_videos.append(video)
            except ValueError:
                continue
    
    # Sort by year (oldest first)
    matching_videos.sort(key=lambda v: v.get('recording_date', ''))
    
    return matching_videos

def get_today_date(timezone_str: str = None) -> tuple:
    """
    Get today's month and day in specified timezone
    
    Args:
        timezone_str: Timezone string (e.g., 'America/New_York')
    
    Returns:
        Tuple of (month, day, formatted_date_string)
    """
    if timezone_str:
        tz = pytz.timezone(timezone_str)
        now = datetime.now(tz)
    else:
        now = datetime.now()
    
    formatted_date = now.strftime('%B %d')  # e.g., "January 4"
    
    return now.month, now.day, formatted_date

def format_video_list_for_reddit(videos: List[Dict]) -> str:
    """
    Format a list of videos for Reddit post
    
    Args:
        videos: List of video dictionaries
    
    Returns:
        Formatted string for Reddit
    """
    if not videos:
        return "*No videos found for this date.*"
    
    lines = []
    for video in videos:
        recording_date = video.get('recording_date', '')
        year = datetime.fromisoformat(recording_date).year if recording_date else 'Unknown'
        title = video.get('title', 'Untitled')
        url = video.get('url', '')
        
        lines.append(f"**{year}:** [{title}]({url})")
    
    return '\n\n'.join(lines)

def get_env_variable(var_name: str, required: bool = True) -> Optional[str]:
    """
    Get environment variable with error handling
    
    Args:
        var_name: Name of environment variable
        required: Whether the variable is required
    
    Returns:
        Variable value or None
    
    Raises:
        Exception if required variable is missing
    """
    value = os.getenv(var_name)
    
    if required and not value:
        raise Exception(f"Required environment variable not set: {var_name}")
    
    return value

def create_backup(file_path: str) -> None:
    """Create a backup of a file"""
    if os.path.exists(file_path):
        backup_path = f"{file_path}.backup"
        import shutil
        shutil.copy2(file_path, backup_path)
        logging.info(f"Created backup: {backup_path}")
