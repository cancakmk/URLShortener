FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Entity Framework CLI araçlarını yükle
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# Projeyi kopyala ve bağımlılıkları yükle
COPY ["UrlShortener.Api.csproj", "./"]
RUN dotnet restore "UrlShortener.Api.csproj"

# Tüm kaynak kodları kopyala ve derle
COPY . .
RUN dotnet build "UrlShortener.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UrlShortener.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create a non-root user
RUN adduser --disabled-password --gecos "" appuser

# Copy the published app
COPY --from=publish /app/publish .

# Migration'ları çalıştırmak için gerekli dosyaları kopyala
COPY --from=build /src/Migrations /app/Migrations
COPY --from=build /src/Data /app/Data
COPY --from=build /src/Models /app/Models

# Set proper permissions
RUN chown -R appuser:appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:5000

# Add security hardening
ENV COMPlus_EnableDiagnostics=0
ENV DOTNET_EnableDiagnostics=0

# Health check
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "UrlShortener.Api.dll"] 