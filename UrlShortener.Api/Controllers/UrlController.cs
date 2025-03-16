using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;
using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.RateLimiting;

namespace UrlShortener.Api.Controllers
{
    /// <summary>
    /// API for URL shortening and redirection operations
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class UrlController : ControllerBase
    {
        private readonly IUrlShortenerService _urlShortenerService;
        private readonly IConfiguration _configuration;

        public UrlController(IUrlShortenerService urlShortenerService, IConfiguration configuration)
        {
            _urlShortenerService = urlShortenerService;
            _configuration = configuration;
        }

        /// <summary>
        /// Shortens a long URL
        /// </summary>
        /// <param name="request">URL information to be shortened</param>
        /// <returns>Shortened URL information</returns>
        /// <response code="200">URL successfully shortened</response>
        /// <response code="400">Invalid URL format</response>
        /// <response code="429">Too many requests</response>
        [HttpPost("shorten")]
        [EnableRateLimiting("ShortenEndpoint")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ShortenUrl([FromBody] UrlRequest request)
        {
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out Uri uriResult) || 
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest("Invalid URL format. Only HTTP and HTTPS URLs are allowed.");
            }

            // Sanitize URL to prevent XSS
            string sanitizedUrl = SanitizeUrl(request.Url);

            var shortenedUrl = await _urlShortenerService.ShortenUrlAsync(sanitizedUrl);
            var baseUrl = _configuration["BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

            return Ok(new
            {
                OriginalUrl = shortenedUrl.OriginalUrl,
                ShortUrl = $"{baseUrl}/{shortenedUrl.ShortCode}",
                ShortCode = shortenedUrl.ShortCode,
                CreatedAt = shortenedUrl.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ClickCount = shortenedUrl.ClickCount,
                LastClickedAt = shortenedUrl.LastClickedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = shortenedUrl.Status.ToString()
            });
        }

        /// <summary>
        /// Redirects to the original URL using the short code
        /// </summary>
        /// <param name="shortCode">Short URL code</param>
        /// <returns>Redirection to the original URL</returns>
        /// <response code="302">Successfully redirected to the original URL</response>
        /// <response code="404">Short URL code not found</response>
        /// <response code="400">URL is inactive or expired</response>
        [HttpGet("{shortCode}")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RedirectToOriginalUrl(string shortCode)
        {
            // Validate shortCode format to prevent injection attacks
            if (!IsValidShortCode(shortCode))
            {
                return BadRequest("Invalid short code format");
            }

            var shortenedUrl = await _urlShortenerService.GetByShortCodeAsync(shortCode);

            if (shortenedUrl == null)
            {
                return NotFound();
            }
            
            if (shortenedUrl.Status != UrlStatus.Active)
            {
                return BadRequest($"This URL is in {shortenedUrl.Status} status and no longer active.");
            }

            await _urlShortenerService.IncrementClickCountAsync(shortCode);

            // Validate URL before redirecting to prevent open redirect vulnerabilities
            if (!Uri.TryCreate(shortenedUrl.OriginalUrl, UriKind.Absolute, out Uri uriResult) || 
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest("Invalid URL format in database. Cannot redirect.");
            }

            return Redirect(shortenedUrl.OriginalUrl);
        }

        /// <summary>
        /// Gets statistics for a short URL
        /// </summary>
        /// <param name="shortCode">Short URL code</param>
        /// <returns>URL statistics</returns>
        /// <response code="200">Statistics successfully retrieved</response>
        /// <response code="404">Short URL code not found</response>
        [HttpGet("stats/{shortCode}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUrlStats(string shortCode)
        {
            var shortenedUrl = await _urlShortenerService.GetByShortCodeAsync(shortCode);

            if (shortenedUrl == null)
            {
                return NotFound();
            }

            var baseUrl = _configuration["BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

            return Ok(new
            {
                OriginalUrl = shortenedUrl.OriginalUrl,
                ShortUrl = $"{baseUrl}/{shortenedUrl.ShortCode}",
                ShortCode = shortenedUrl.ShortCode,
                CreatedAt = shortenedUrl.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ClickCount = shortenedUrl.ClickCount,
                LastClickedAt = shortenedUrl.LastClickedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = shortenedUrl.Status.ToString()
            });
        }

        /// <summary>
        /// Updates the status of a short URL
        /// </summary>
        /// <param name="shortCode">Short URL code</param>
        /// <param name="request">New status information</param>
        /// <returns>Updated URL information</returns>
        /// <response code="200">Status successfully updated</response>
        /// <response code="404">Short URL code not found</response>
        [HttpPut("{shortCode}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUrlStatus(string shortCode, [FromBody] UrlStatusUpdateRequest request)
        {
            // Validate shortCode format to prevent injection attacks
            if (!IsValidShortCode(shortCode))
            {
                return BadRequest("Invalid short code format");
            }

            var shortenedUrl = await _urlShortenerService.GetByShortCodeAsync(shortCode);

            if (shortenedUrl == null)
            {
                return NotFound();
            }

            await _urlShortenerService.UpdateUrlStatusAsync(shortCode, request.Status);
            
            shortenedUrl = await _urlShortenerService.GetByShortCodeAsync(shortCode);
            var baseUrl = _configuration["BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

            return Ok(new
            {
                OriginalUrl = shortenedUrl.OriginalUrl,
                ShortUrl = $"{baseUrl}/{shortenedUrl.ShortCode}",
                ShortCode = shortenedUrl.ShortCode,
                CreatedAt = shortenedUrl.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ClickCount = shortenedUrl.ClickCount,
                LastClickedAt = shortenedUrl.LastClickedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = shortenedUrl.Status.ToString()
            });
        }

        /// <summary>
        /// Validates if the short code format is valid
        /// </summary>
        private bool IsValidShortCode(string shortCode)
        {
            // Only allow alphanumeric characters, hyphens, and underscores with length 8
            return !string.IsNullOrEmpty(shortCode) && 
                   shortCode.Length == 8 && 
                   Regex.IsMatch(shortCode, "^[a-zA-Z0-9_-]+$");
        }

        /// <summary>
        /// Sanitizes a URL to prevent XSS attacks
        /// </summary>
        private string SanitizeUrl(string url)
        {
            // Remove potentially dangerous characters
            return Regex.Replace(url, @"[<>""']", "");
        }
    }
} 