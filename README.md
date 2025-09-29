# Keynote ASP.NET Backend - Real-time PDF Presentation API

<div align="center">
  <img src="readme-image/banner_dark.svg" alt="Keynote Banner" width="400" />
</div>

A robust ASP.NET Core backend service that powers the Keynote real-time PDF presentation platform. Built with .NET 10, Entity Framework Core, and SignalR for seamless real-time communication.

## Overview

The Keynote backend provides a comprehensive API for managing PDF presentations, user authentication, and real-time synchronization. It integrates with NAUTH for secure authentication and offers powerful features for presentation management and audience interaction.

## Key Features

### Presentation Management

- **PDF Processing**: Secure PDF upload and processing with metadata extraction
- **Real-time Control**: SignalR-powered presentation synchronization across all devices
- **Session Management**: Advanced session handling with automatic cleanup
- **Unkillable Sessions**: Sessions persist across device reboots via secure cookies
- **Presenter Tools**: Private notes and presentation control features

### Interactive Features

- **Raise Hand**: Real-time audience interaction and question management
- **Temporary Control**: Grant audience members temporary presentation control
- **Live Updates**: Instant synchronization of presentation state changes
- **Mobile-Optimized Viewing**: Spectators get a mobile-friendly presentation version
- **Accessibility Options**: Spectators can switch to screen version for better visibility

### Technical Capabilities

- **SignalR Hubs**: Multiple specialized hubs for different user types (Presenter, Spectator, Screen)
- **Entity Framework Core**: Robust data persistence with automatic migrations
- **AutoMapper**: Efficient object mapping and DTO transformations
- **Repository Pattern**: Clean separation of concerns with generic repository implementation

### Authentication & Security

- **NAUTH Integration**: Secure authentication via NAUTH microservice
- **Authorization Handlers**: Custom authorization requirements and handlers
- **Permission System**: Fine-grained access control with role-based permissions
- **JWT Token Validation**: Secure token-based authentication

## Architecture

The backend follows a clean architecture pattern with the following components:

### Controllers
- **KeynoteController**: Main presentation management endpoints
- **SessionController**: Session lifecycle management
- **UserController**: User-related operations
- **StatusController**: Health check and system status

### Services
- **KeynoteService**: Core presentation business logic
- **UserService**: User management and authentication
- **PresentorService**: Presenter-specific functionality
- **SpectatorService**: Audience interaction features
- **SignalRRefreshService**: Real-time communication management

### SignalR Hubs
- **PresentorHub**: Real-time communication for presenters
- **SpectatorHub**: Audience interaction and synchronization
- **ScreenHub**: Display device management

### Data Layer
- **KeynoteDbContext**: Entity Framework Core context
- **Repositories**: Generic and specialized data access patterns
- **Migrations**: Database schema management

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server or compatible database
- NAUTH authentication service running
- Docker (optional, for containerized deployment)

### Installation

1. Clone the repository and navigate to the project directory:
```bash
cd keynote-asp
```

2. Restore NuGet packages:
```bash
dotnet restore
```

3. Update the connection string in `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your connection string here"
  }
}
```

4. Run database migrations:
```bash
dotnet ef database update
```

5. Start the development server:
```bash
dotnet run
```

The API will be available at `https://localhost:5001` (HTTPS) or `http://localhost:5000` (HTTP).

### Environment Configuration

Configure the following settings in your `appsettings.json`:

- **ConnectionStrings**: Database connection configuration
- **NAUTH**: Authentication service endpoints and settings
- **SignalR**: Real-time communication configuration
- **Logging**: Logging levels and providers

## API Documentation

The API includes comprehensive Swagger documentation available at:
- Development: `https://localhost:5001/swagger`
- Production: `https://your-domain.com/swagger`

### Key Endpoints

#### Presentations
- `GET /api/keynote` - Get user's presentations
- `POST /api/keynote` - Create new presentation
- `PUT /api/keynote/{id}` - Update presentation
- `DELETE /api/keynote/{id}` - Delete presentation

#### Sessions
- `POST /api/session/start` - Start presentation session
- `POST /api/session/end` - End presentation session
- `GET /api/session/status` - Get session status
- **Persistent Sessions**: Sessions automatically reconnect after device reboots using secure cookies

#### Real-time Communication
- SignalR hubs for real-time presentation synchronization
- WebSocket connections for live updates
- Event-driven architecture for instant notifications

## Development

### Project Structure

```
keynote-asp/
├── Controllers/          # API controllers
├── Services/            # Business logic services
├── SignalRHubs/         # Real-time communication hubs
├── Models/              # Data models and DTOs
├── Repositories/        # Data access layer
├── DbContexts/          # Entity Framework contexts
├── AuthHandlers/        # Authentication and authorization
├── Mappings/            # AutoMapper configurations
├── Migrations/          # Database migrations
└── Helpers/             # Utility classes and extensions
```

### Code Generation

The project includes PowerShell scripts for API client generation:

- `Scripts/generate-api.ps1` - Generate API client from OpenAPI specification

### Database Migrations

Create new migrations:
```bash
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

## Deployment

### Docker

Build and run with Docker:
```bash
docker build -t keynote-asp .
docker run -p 5000:80 keynote-asp
```

### Production Configuration

1. Update `appsettings.Production.json` with production settings
2. Configure SSL certificates for HTTPS
3. Set up reverse proxy (nginx, IIS, etc.)
4. Configure logging and monitoring
5. Set up database backups and maintenance

## Related Projects

- **Keynote Frontend**: [https://github.com/nozsavsev/keynote-frontend](https://github.com/nozsavsev/keynote-frontend) - Next.js frontend application
- **NAUTH Backend**: [https://github.com/nozsavsev/nauth-asp](https://github.com/nozsavsev/nauth-asp) - Authentication and authorization microservice
- **NAUTH Frontend**: [https://github.com/nozsavsev/nauth-frontend](https://github.com/nozsavsev/nauth-frontend) - Authentication service frontend

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

© 2024 Keynote. All rights reserved.
