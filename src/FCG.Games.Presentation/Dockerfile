# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000
EXPOSE 5001


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/FCG.Games.Presentation/FCG.Games.Presentation.csproj", "src/FCG.Games.Presentation/"]
COPY ["src/FCG.Games.Domain/FCG.Games.Domain.csproj", "src/FCG.Games.Domain/"]
COPY ["src/FCG.Games.Infrastructure/FCG.Games.Infrastructure.csproj", "src/FCG.Games.Infrastructure/"]
COPY ["src/FCG.Games.Service/FCG.Games.Service.csproj", "src/FCG.Games.Service/"]
RUN dotnet restore "./src/FCG.Games.Presentation/FCG.Games.Presentation.csproj"
COPY . .
WORKDIR "/src/src/FCG.Games.Presentation"
RUN dotnet build "./FCG.Games.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FCG.Games.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FCG.Games.Presentation.dll"]