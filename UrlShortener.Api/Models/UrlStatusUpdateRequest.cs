using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.Models
{
    /// <summary>
    /// Request model for updating URL status
    /// </summary>
    public class UrlStatusUpdateRequest
    {
        /// <summary>
        /// New status for the URL
        /// </summary>
        [Required]
        public UrlStatus Status { get; set; }
    }
} 