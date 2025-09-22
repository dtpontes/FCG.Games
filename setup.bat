@echo off
REM FCG Games Microservice Setup Script for Windows

echo === FCG Games Microservice Setup ===
echo.

REM Check if Docker is running
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo ? Docker is not running. Please start Docker and try again.
    exit /b 1
)

echo ? Docker is running

REM Check if docker-compose is available
where docker-compose >nul 2>&1
if %errorlevel% neq 0 (
    echo ? docker-compose is not installed. Please install docker-compose and try again.
    exit /b 1
)

echo ? docker-compose is available

REM Parse command line arguments
set COMMAND=%1
if "%COMMAND%"=="" set COMMAND=dev

if "%COMMAND%"=="dev" goto :dev
if "%COMMAND%"=="prod" goto :prod
if "%COMMAND%"=="stop" goto :stop
if "%COMMAND%"=="logs" goto :logs
if "%COMMAND%"=="health" goto :health
if "%COMMAND%"=="clean" goto :clean
if "%COMMAND%"=="rebuild" goto :rebuild
goto :usage

:dev
echo.
echo ?? Starting FCG Games Microservice in Development mode...
docker-compose up -d
echo.
echo ? Services started successfully!
echo.
echo ?? Available endpoints:
echo    • Games API (HTTP):  http://localhost:5001
echo    • Games API (HTTPS): https://localhost:5002
echo    • Swagger UI:         http://localhost:5001/swagger
echo    • Health Check:       http://localhost:5001/health
echo    • GraphQL:           http://localhost:5001/graphql
echo.
echo ?? View logs: %~nx0 logs
echo ?? Stop services: %~nx0 stop
goto :end

:prod
echo.
echo ?? Starting FCG Games Microservice in Production mode...
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
echo.
echo ? Production services started successfully!
goto :end

:stop
echo.
echo ?? Stopping all services...
docker-compose down
echo ? All services stopped
goto :end

:logs
echo.
echo ?? Showing service logs (Press Ctrl+C to exit)...
docker-compose logs -f games-service
goto :end

:health
echo.
echo ?? Checking service health...
echo.
echo Games Service Health:
curl -s http://localhost:5001/health
echo.
echo.
echo Container Status:
docker-compose ps
goto :end

:clean
echo.
echo ?? Cleaning up all containers and volumes...
docker-compose down -v
docker system prune -f
echo ? Cleanup completed
goto :end

:rebuild
echo.
echo ?? Rebuilding and restarting services...
docker-compose down
docker-compose build --no-cache
docker-compose up -d
echo ? Services rebuilt and restarted
goto :end

:usage
echo.
echo ? Unknown command: %COMMAND%
echo.
echo Usage: %~nx0 [COMMAND]
echo.
echo Commands:
echo   dev       Start development environment
echo   prod      Start production environment
echo   stop      Stop all services
echo   logs      Show service logs
echo   health    Check service health
echo   clean     Remove all containers and volumes
echo   rebuild   Rebuild and restart services
echo.
exit /b 1

:end