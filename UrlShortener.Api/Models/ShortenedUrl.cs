using System;

namespace UrlShortener.Api.Models
{
    /// <summary>
    /// Represents a shortened URL
    /// </summary>
    public class ShortenedUrl
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Original long URL
        /// </summary>
        public required string OriginalUrl { get; set; }
        
        /// <summary>
        /// Short code for the URL
        /// </summary>
        public required string ShortCode { get; set; }
        
        /// <summary>
        /// Creation date and time
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Number of clicks on the shortened URL
        /// </summary>
        public int ClickCount { get; set; }
        
        /// <summary>
        /// Date and time of the last click
        /// </summary>
        public DateTime? LastClickedAt { get; set; }
        
        /// <summary>
        /// Current status of the URL
        /// </summary>
        public UrlStatus Status { get; set; } = UrlStatus.Active;
    }

    /// <summary>
    /// Possible statuses for a shortened URL
    /// </summary>
    public enum UrlStatus
    {
        /// <summary>
        /// URL is active and can be accessed
        /// </summary>
        Active = 0,
        
        /// <summary>
        /// URL is inactive and cannot be accessed
        /// </summary>
        Inactive = 1,
        
        /// <summary>
        /// URL has expired and cannot be accessed
        /// </summary>
        Expired = 2
    }
} 