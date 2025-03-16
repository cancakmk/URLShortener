using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace UrlShortener.Api.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Yapılandırma dosyasını yükle
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // DbContext seçeneklerini yapılandır
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            
            // Yerel geliştirme için bağlantı dizesi
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                "Host=localhost;Database=urlshortener;Username=postgres;Password=postgres";
            
            optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
} 