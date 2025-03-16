using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Services;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Helpers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.CookiePolicy;

var builder = WebApplication.CreateBuilder(args);

// Port yapılandırması
builder.WebHost.UseUrls("http://*:5000");  // Docker içinde 5000 portunda çalışacak şekilde ayarla

// Add security headers service
builder.Services.AddAntiforgery();

// Configure cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Add specific rate limits for certain endpoints
    options.AddPolicy("ShortenEndpoint", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateTimeConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"), name: "postgres")
    .AddRedis(builder.Configuration.GetConnectionString("RedisConnection"), name: "redis");

// Swagger yapılandırması
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "URL Shortener API",
        Version = "v1",
        Description = "RESTful API for URL shortening and redirection operations",
        Contact = new OpenApiContact
        {
            Name = "URL Shortener Team",
            Email = "info@urlshortener.com",
            Url = new Uri("https://github.com/yourusername/urlshortener")
        }
    });
    
    // XML belgeleri ekle (opsiyonel)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// PostgreSQL bağlantısı
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Redis bağlantısı
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
// Redis bağlantısını ConfigurationOptions kullanarak yapılandır
var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
redisOptions.AbortOnConnectFail = false; // Bağlantı hatalarında uygulamanın çökmesini engelle
builder.Services.AddSingleton<IConnectionMultiplexer>(provider => 
    ConnectionMultiplexer.Connect(redisOptions));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// URL kısaltma servisi
builder.Services.AddScoped<IUrlShortenerService, UrlShortenerService>();

var app = builder.Build();

// Veritabanı migration'larını otomatik uygula
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AppDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "URL Shortener API v1");
        c.RoutePrefix = "swagger";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.DefaultModelsExpandDepth(1);
        c.EnableDeepLinking();
        c.DisplayRequestDuration();
    });
}
else
{
    // Add HSTS in production
    app.UseHsts();
    
    // Üretim ortamında da Swagger'ı etkinleştirmek için
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "URL Shortener API v1");
        c.RoutePrefix = "swagger";
    });
}

// Add security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});

// Use cookie policy
app.UseCookiePolicy();

// Use rate limiting
app.UseRateLimiter();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

// Kök URL'yi Swagger'a yönlendir
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run(); 