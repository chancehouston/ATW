using Reddit;
using Reddit.Controllers;
using Microsoft.Extensions.Logging;

namespace AdamTheWoo.Services;

public class RedditService
{
    private readonly RedditClient _reddit;
    private readonly ILogger _logger;

    public RedditService(string clientId, string clientSecret, string username, string password,
        string userAgent, ILogger logger)
    {
        _reddit = new RedditClient(
            appId: clientId,
            appSecret: clientSecret,
            refreshToken: null,
            accessToken: null,
            userAgent: userAgent
        );

        // Authenticate
        _reddit.Account.Login(username, password);

        _logger = logger;
    }

    public string CreatePost(string subredditName, string title, string body, string? flairText = null)
    {
        try
        {
            var subreddit = _reddit.Subreddit(subredditName);

            // Create post
            var post = subreddit.SelfPost(title, body);

            // Add flair if specified
            if (!string.IsNullOrEmpty(flairText))
            {
                try
                {
                    // Note: The Reddit library may need specific implementation for flair
                    // This is a placeholder - actual flair implementation may vary
                    _logger.LogInformation("Flair requested: {FlairText}", flairText);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not apply flair: {Error}", ex.Message);
                }
            }

            var postUrl = $"https://www.reddit.com{post.Permalink}";
            _logger.LogInformation("Post created: {Url}", postUrl);

            return postUrl;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating Reddit post: {ex.Message}");
        }
    }
}
