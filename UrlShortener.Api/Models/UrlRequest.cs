using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.Models
{
    /// <summary>
    /// Request model for URL shortening
    /// </summary>
    public class UrlRequest
    {
        /// <summary>
        /// The URL to be shortened
        /// </summary>
        [Required]
        [Url]
        public required string Url { get; set; }
    }
} 