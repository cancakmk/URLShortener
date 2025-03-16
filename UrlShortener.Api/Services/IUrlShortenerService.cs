using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services
{
    /// <summary>
    /// Service for URL shortening operations
    /// </summary>
    public interface IUrlShortenerService
    {
        /// <summary>
        /// Shortens a URL
        /// </summary>
        /// <param name="originalUrl">The original URL to shorten</param>
        /// <returns>The shortened URL information</returns>
        Task<ShortenedUrl> ShortenUrlAsync(string originalUrl);
        
        /// <summary>
        /// Gets a shortened URL by its short code
        /// </summary>
        /// <param name="shortCode">The short code to look up</param>
        /// <returns>The shortened URL information if found, null otherwise</returns>
        Task<ShortenedUrl> GetByShortCodeAsync(string shortCode);
        
        /// <summary>
        /// Increments the click count for a shortened URL
        /// </summary>
        /// <param name="shortCode">The short code of the URL</param>
        /// <returns>The updated shortened URL information</returns>
        Task<ShortenedUrl> IncrementClickCountAsync(string shortCode);
        
        /// <summary>
        /// Updates the status of a shortened URL
        /// </summary>
        /// <param name="shortCode">The short code of the URL</param>
        /// <param name="status">The new status</param>
        /// <returns>The updated shortened URL information</returns>
        Task<ShortenedUrl> UpdateUrlStatusAsync(string shortCode, UrlStatus status);
    }
} 