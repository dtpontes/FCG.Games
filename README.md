# FCG Games Microservice

A .NET 9 microservice for game management with Azure integration, Entity Framework, and GraphQL support.

## 🏗️ Architecture

- **Framework**: .NET 9
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT with ASP.NET Core Identity
- **Messaging**: Azure Service Bus
- **API**: REST Controllers + GraphQL
- **Documentation**: Swagger/OpenAPI
- **Containerization**: Docker

## 🚀 Azure Deployment Guide

### Prerequisites

- Azure CLI installed and authenticated (`az login`)
- Docker Hub account with the image `dtpontes/fcggamespresentation:latest`
- Azure subscription with sufficient quota
- Existing Azure Service Bus namespace and queue (external setup)

### Deployment Steps

#### 1. Create Resource Group
```bash
az group create --name RGFCGGames --location westus2
```

#### 2. Create SQL Server
```bash
az sql server create --name fcg-games-sqlserver --resource-group RGFCGGames --location westus2 --admin-user sqladmin --admin-password YourStrongPassword123
```

#### 3. Create SQL Database
```bash
az sql db create --resource-group RGFCGGames --server fcg-games-sqlserver --name FCGGamesDatabase --service-objective S0
```

#### 4. Configure SQL Server Firewall
```bash
az sql server firewall-rule create --resource-group RGFCGGames --server fcg-games-sqlserver --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

#### 5. Create Container Apps Environment
```bash
az containerapp env create --name fcg-games-env --resource-group RGFCGGames --location westus2
```

#### 6. Deploy Application Container
```bash
az containerapp create --name fcg-games-app --resource-group RGFCGGames --environment fcg-games-env --image dtpontes/fcggamespresentation:latest --target-port 8080 --ingress external --env-vars ASPNETCORE_ENVIRONMENT=Production
```

#### 7. Configure Database Connection
```bash
az containerapp update --name fcg-games-app --resource-group RGFCGGames --set-env-vars "ConnectionStrings__DefaultConnection=Server=fcg-games-sqlserver.database.windows.net;Database=FCGGamesDatabase;User=sqladmin;Password=YourStrongPassword123;TrustServerCertificate=True;Encrypt=True;"
```

#### 8. Configure Service Bus Connection
```bash
az containerapp update --name fcg-games-app --resource-group RGFCGGames --set-env-vars "ServiceBus__ConnectionString=Endpoint=sb://fcg-games-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=83uilbws19wJIBUXcNOqNaDylJ9oFiDEv+ASbHpvwmc="
```

#### 9. Configure Service Bus Settings
```bash
az containerapp update --name fcg-games-app --resource-group RGFCGGames --set-env-vars ServiceBus__SalesQueueName=sale-processing-queue ServiceBus__MaxConcurrentCalls=5 ServiceBus__MessageTimeoutSeconds=300
```

#### 10. Configure JWT Settings
```bash
az containerapp update --name fcg-games-app --resource-group RGFCGGames --set-env-vars "JwtSettings__Issuer=FCG.Presentation" "JwtSettings__Audience=FCG.WebApp" "JwtSettings__SecretKey=MyPresentationSecretKey123456789"
```

### 🔍 Verify Deployment

After deployment, verify the application is running:

```bash
# Get the application URL
az containerapp show --name fcg-games-app --resource-group RGFCGGames --query properties.configuration.ingress.fqdn
```

### 📋 Application Endpoints

- **Application Base URL**: `https://fcg-games-app.[environment-id].westus2.azurecontainerapps.io`
- **Swagger Documentation**: `/swagger`
- **Health Check**: `/health`
- **GraphQL Endpoint**: `/graphql`
- **Service Bus Monitor**: `/api/servicebus/test-connection`

### 🛠️ Local Development

#### Prerequisites
- .NET 9 SDK
- Docker Desktop
- SQL Server (local or Docker)
- Azure Service Bus (or local emulator)

#### Run with Docker Compose
```bash
docker-compose up -d
```

#### Run Locally
```bash
dotnet restore
dotnet run --project src/FCG.Games.Presentation
```

### 🗂️ Project Structure

```
├── src/
│   ├── FCG.Games.Domain/          # Domain entities and business logic
│   ├── FCG.Games.Infrastructure/  # Data access and external services
│   ├── FCG.Games.Service/         # Application services
│   └── FCG.Games.Presentation/    # Web API and controllers
├── docker-compose.yml             # Docker composition
├── docker-compose.override.yml    # Development overrides
└── README.md                      # This file
```

### 🔒 Security Notes

- **Change default passwords in production**
- **Use Azure Key Vault for secrets management**
- **Configure proper CORS policies for production**
- **Enable HTTPS and security headers**
- **Generate a secure JWT secret key for production**

### ⚠️ Important Notes

- **Service Bus**: Assumes an existing Azure Service Bus namespace `fcg-games-servicebus` with queue `sale-processing-queue`
- **JWT Configuration**: Step 10 is critical for authentication to work properly
- **Health Checks**: Monitor `/health` endpoint to ensure all services are running
- **Database**: Migrations will run automatically on first startup

### 🧹 Cleanup Resources

To remove all Azure resources:

```bash
az group delete --name RGFCGGames --yes --no-wait
```

### 📞 Support

For issues and questions, please refer to the project repository issues section.

---

**Technologies Used**: .NET 9, Entity Framework Core, Azure Container Apps, Azure SQL Database, Azure Service Bus, GraphQL, JWT Authentication, Docker