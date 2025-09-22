@echo off
REM Docker Compose Troubleshooting Script for FCG Games

echo === FCG Games Docker Troubleshooting ===
echo.

REM Parse command line arguments
set ACTION=%1
if "%ACTION%"=="" set ACTION=diagnose

if "%ACTION%"=="diagnose" goto :diagnose
if "%ACTION%"=="clean" goto :clean
if "%ACTION%"=="restart" goto :restart
if "%ACTION%"=="logs" goto :logs
if "%ACTION%"=="sqlserver-logs" goto :sqlserver-logs
if "%ACTION%"=="test-connection" goto :test-connection
goto :usage

:check_docker
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo ? Docker is not running. Please start Docker Desktop.
    exit /b 1
)
echo ? Docker is running
goto :eof

:diagnose
call :check_docker
if %errorlevel% neq 0 exit /b 1

echo.
echo ?? Container health status:
docker-compose ps

echo.
echo ?? Detailed container status:
docker ps -a --filter "name=fcg-games"

echo.
echo ?? SQL Server container logs:
docker-compose logs sqlserver

echo.
echo ?? Testing SQL Server connection...
docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "%SA_PASSWORD%" -Q "SELECT @@VERSION" -C
goto :end

:clean
call :check_docker
if %errorlevel% neq 0 exit /b 1

echo.
echo ?? Cleaning up existing containers and volumes...
docker-compose down -v --remove-orphans
docker system prune -f
echo ? Cleanup completed
goto :end

:restart
call :check_docker
if %errorlevel% neq 0 exit /b 1

echo.
echo ?? Cleaning up existing containers...
docker-compose down -v --remove-orphans

echo.
echo ?? Restarting containers with verbose logging...
docker-compose up -d --force-recreate

echo.
echo ?? Following logs (Press Ctrl+C to stop):
docker-compose logs -f
goto :end

:logs
echo ?? Container logs:
docker-compose logs
goto :end

:sqlserver-logs
echo ?? SQL Server container logs:
docker-compose logs sqlserver
goto :end

:test-connection
echo ?? Testing SQL Server connection...
docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "%SA_PASSWORD%" -Q "SELECT @@VERSION" -C
goto :end

:usage
echo Usage: %~nx0 [diagnose^|clean^|restart^|logs^|sqlserver-logs^|test-connection]
echo.
echo Commands:
echo   diagnose         Run full diagnostics (default)
echo   clean            Clean up containers and volumes
echo   restart          Clean restart with logs
echo   logs             Show all container logs
echo   sqlserver-logs   Show SQL Server logs only
echo   test-connection  Test SQL Server connection
goto :end

:end