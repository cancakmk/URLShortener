# URL Shortener

A modern URL shortening service built with ASP.NET Core, PostgreSQL, and Redis.

## Features

- **URL Shortening**: Convert long URLs into short, easy-to-share links
- **URL Redirection**: Redirect users from short links to original URLs
- **Click Tracking**: Track the number of clicks on each shortened URL
- **URL Statistics**: View detailed statistics for each shortened URL
- **URL Status Management**: Enable/disable shortened URLs

## Tech Stack

- **Backend**: ASP.NET Core 8.0
- **Database**: PostgreSQL 14
- **Caching**: Redis
- **Containerization**: Docker & Docker Compose

## Prerequisites

- [Docker](https://www.docker.com/products/docker-desktop/)
- [Docker Compose](https://docs.docker.com/compose/install/)

## Getting Started

### Running with Docker Compose

1. Clone the repository:
   ```bash
   git clone https://github.com/cancakmk/URLShortener
   cd URLShortener
   ```

2. Start the application:
   ```bash
   docker-compose up -d
   ```

3. The application will be available at:
   - API: http://localhost:5000

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/shorten` | Shorten a URL |
| GET | `/{shortCode}` | Redirect to the original URL |
| GET | `/stats/{shortCode}` | Get statistics for a shortened URL |
| PUT | `/{shortCode}/status` | Update the status of a shortened URL |

## API Usage Examples

### Shorten a URL

```bash
curl -X POST http://localhost:5000/shorten \
  -H "Content-Type: application/json" \
  -d '{"url": "https://example.com/very/long/url/that/needs/shortening"}'
```

Response:
```json
{
  "originalUrl": "https://example.com/very/long/url/that/needs/shortening",
  "shortUrl": "http://localhost:5000/abc123",
  "shortCode": "abc123",
  "createdAt": "2023-03-16 20:30:45",
  "clickCount": 0,
  "lastClickedAt": null
}
```

### Get URL Statistics

```bash
curl -X GET http://localhost:5000/stats/abc123
```

Response:
```json
{
  "originalUrl": "https://example.com/very/long/url/that/needs/shortening",
  "shortUrl": "http://localhost:5000/abc123",
  "shortCode": "abc123",
  "createdAt": "2023-03-16 20:30:45",
  "clickCount": 5,
  "lastClickedAt": "2023-03-16 21:15:30",
  "isActive": true
}
```

### Update URL Status

```bash
curl -X PUT http://localhost:5000/abc123/status \
  -H "Content-Type: application/json" \
  -d '{"isActive": false}'
```

Response:
```json
{
  "originalUrl": "https://example.com/very/long/url/that/needs/shortening",
  "shortUrl": "http://localhost:5000/abc123",
  "shortCode": "abc123",
  "createdAt": "2023-03-16 20:30:45",
  "clickCount": 5,
  "lastClickedAt": "2023-03-16 21:15:30",
  "isActive": false
}
```

## Project Structure

```
URLShortener/
├── UrlShortener.Api/       # ASP.NET Core API project
│   ├── Controllers/        # API controllers
│   ├── Data/               # Database context and migrations
│   ├── Models/             # Data models
│   ├── Services/           # Business logic services
│   └── Helpers/            # Helper classes and utilities
├── docker-compose.yml      # Docker Compose configuration
└── README.md               # Project documentation
```

## Configuration

The application can be configured through environment variables in the `docker-compose.yml` file:

- `ASPNETCORE_ENVIRONMENT`: Set to `Development` or `Production`
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string
- `ConnectionStrings__RedisConnection`: Redis connection string
- `BaseUrl`: Base URL for generating shortened URLs

## License

This project is licensed under the MIT License - see the LICENSE file for details. 
