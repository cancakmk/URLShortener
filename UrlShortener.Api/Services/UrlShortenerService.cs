using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services
{
    public class UrlShortenerService : IUrlShortenerService
    {
        private readonly AppDbContext _dbContext;
        private readonly ICacheService _cacheService;
        private const string CacheKeyPrefix = "url_";

        public UrlShortenerService(AppDbContext dbContext, ICacheService cacheService)
        {
            _dbContext = dbContext;
            _cacheService = cacheService;
        }

        public async Task<ShortenedUrl> ShortenUrlAsync(string originalUrl)
        {
            var existingUrl = await _dbContext.ShortenedUrls
                .FirstOrDefaultAsync(u => u.OriginalUrl == originalUrl);

            if (existingUrl != null)
            {
                return existingUrl;
            }

            var shortCode = await GenerateUniqueShortCodeAsync(originalUrl);

            var shortenedUrl = new ShortenedUrl
            {
                OriginalUrl = originalUrl,
                ShortCode = shortCode,
                CreatedAt = DateTime.UtcNow,
                ClickCount = 0
            };

            _dbContext.ShortenedUrls.Add(shortenedUrl);
            await _dbContext.SaveChangesAsync();

            await _cacheService.SetAsync($"{CacheKeyPrefix}{shortCode}", shortenedUrl, TimeSpan.FromDays(7));

            return shortenedUrl;
        }

        public async Task<ShortenedUrl> GetByShortCodeAsync(string shortCode)
        {
            var cachedUrl = await _cacheService.GetAsync<ShortenedUrl>($"{CacheKeyPrefix}{shortCode}");
            if (cachedUrl != null)
            {
                return cachedUrl;
            }

            var shortenedUrl = await _dbContext.ShortenedUrls
                .FirstOrDefaultAsync(u => u.ShortCode == shortCode);

            if (shortenedUrl != null)
            {
                await _cacheService.SetAsync($"{CacheKeyPrefix}{shortCode}", shortenedUrl, TimeSpan.FromDays(7));
            }

            return shortenedUrl;
        }

        public async Task<ShortenedUrl> IncrementClickCountAsync(string shortCode)
        {
            var shortenedUrl = await _dbContext.ShortenedUrls
                .FirstOrDefaultAsync(u => u.ShortCode == shortCode);
            
            if (shortenedUrl != null)
            {
                shortenedUrl.ClickCount++;
                shortenedUrl.LastClickedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                
                await _cacheService.SetAsync($"{CacheKeyPrefix}{shortCode}", shortenedUrl, TimeSpan.FromDays(7));
            }
            
            return shortenedUrl;
        }

        public async Task<ShortenedUrl> UpdateUrlStatusAsync(string shortCode, UrlStatus status)
        {
            var shortenedUrl = await _dbContext.ShortenedUrls
                .FirstOrDefaultAsync(u => u.ShortCode == shortCode);
            
            if (shortenedUrl != null)
            {
                shortenedUrl.Status = status;
                await _dbContext.SaveChangesAsync();
                
                await _cacheService.SetAsync($"{CacheKeyPrefix}{shortCode}", shortenedUrl, TimeSpan.FromDays(7));
            }
            
            return shortenedUrl;
        }

        private async Task<string> GenerateUniqueShortCodeAsync(string url)
        {
            using var sha256 = SHA256.Create();
            var urlBytes = Encoding.UTF8.GetBytes(url + Guid.NewGuid().ToString());
            var hashBytes = sha256.ComputeHash(urlBytes);
            
            var base64 = Convert.ToBase64String(hashBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            var shortCode = base64.Substring(0, 8);

            while (await _dbContext.ShortenedUrls.AnyAsync(u => u.ShortCode == shortCode))
            {
                urlBytes = Encoding.UTF8.GetBytes(url + Guid.NewGuid().ToString());
                hashBytes = sha256.ComputeHash(urlBytes);
                base64 = Convert.ToBase64String(hashBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
                shortCode = base64.Substring(0, 8);
            }

            return shortCode;
        }
    }
} 