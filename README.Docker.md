# FCG Games Microservice

## Docker Compose Setup

This microservice is part of the FCG microservices architecture and runs on dedicated ports to avoid conflicts with other services.

### Ports
- **HTTP**: 5001
- **HTTPS**: 5002
- **SQL Server**: 1433

### Quick Start

#### Development Environment
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f games-service

# Stop services
docker-compose down
```

#### Production Environment
```bash
# Use production configuration
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Available Services

| Service | URL | Description |
|---------|-----|-------------|
| Games API | http://localhost:5001 | Main API endpoint |
| Games API (HTTPS) | https://localhost:5002 | Secure API endpoint |
| Swagger UI | http://localhost:5001/swagger | API documentation |
| Health Check | http://localhost:5001/health | Service health status |
| GraphQL | http://localhost:5001/graphql | GraphQL endpoint |

### Environment Variables

Key environment variables can be configured in the `.env` file:

- `SA_PASSWORD`: SQL Server SA password
- `DATABASE_NAME`: Database name
- `ASPNETCORE_ENVIRONMENT`: Application environment
- `JWT_SECRET_KEY`: JWT signing key
- `GAMES_SERVICE_HTTP_PORT`: HTTP port for the service
- `GAMES_SERVICE_HTTPS_PORT`: HTTPS port for the service

### Database

The service automatically:
- Applies Entity Framework migrations on startup
- Seeds initial data (roles, etc.)
- Uses SQL Server 2022 in a container

### Health Checks

The service includes comprehensive health checks:
- Database connectivity
- Entity Framework DbContext status

Access health status at: `http://localhost:5001/health`

### Useful Commands

```bash
# Rebuild the API container
docker-compose build games-service

# Execute commands in the database
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "YourStrong@Passw0rd"

# View container status
docker-compose ps

# View service logs
docker-compose logs games-service

# Scale the service (production)
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --scale games-service=3
```

### Integration with Other Microservices

This service is designed to work within a microservices architecture. The Docker network `fcg-microservices-network` allows communication between services.

To integrate with other FCG microservices, ensure they use:
- The same Docker network: `fcg-microservices-network`
- Different port ranges to avoid conflicts
- Consistent service discovery patterns
```