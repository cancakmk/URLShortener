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
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000

# Migration'ları çalıştırmak için gerekli dosyaları kopyala
COPY --from=build /src/Migrations /app/Migrations
COPY --from=build /src/Data /app/Data
COPY --from=build /src/Models /app/Models

ENTRYPOINT ["dotnet", "UrlShortener.Api.dll"] 