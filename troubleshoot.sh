#!/bin/bash

# Docker Compose Troubleshooting Script for FCG Games

echo "=== FCG Games Docker Troubleshooting ==="
echo ""

# Load environment variables
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
fi

# Function to check Docker health
check_docker_health() {
    echo "?? Checking Docker status..."
    if ! docker info > /dev/null 2>&1; then
        echo "? Docker is not running. Please start Docker and try again."
        return 1
    fi
    echo "? Docker is running"
    return 0
}

# Function to clean up existing containers
cleanup_containers() {
    echo ""
    echo "?? Cleaning up existing containers and volumes..."
    docker-compose down -v --remove-orphans
    docker system prune -f
    echo "? Cleanup completed"
}

# Function to check SQL Server container logs
check_sqlserver_logs() {
    echo ""
    echo "?? SQL Server container logs:"
    docker-compose logs sqlserver
}

# Function to test SQL Server connection
test_sqlserver_connection() {
    echo ""
    echo "?? Testing SQL Server connection..."
    docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "${SA_PASSWORD}" -Q "SELECT @@VERSION" -C 2>/dev/null
    if [ $? -eq 0 ]; then
        echo "? SQL Server connection successful"
    else
        echo "? SQL Server connection failed"
    fi
}

# Function to check container health
check_container_health() {
    echo ""
    echo "?? Container health status:"
    docker-compose ps
    echo ""
    echo "?? Detailed container status:"
    docker ps -a --filter "name=fcg-games"
}

# Function to restart with verbose logging
restart_with_logs() {
    echo ""
    echo "?? Restarting containers with verbose logging..."
    docker-compose up -d --force-recreate
    echo ""
    echo "?? Following logs (Press Ctrl+C to stop):"
    docker-compose logs -f
}

# Main troubleshooting flow
main() {
    local action=${1:-diagnose}
    
    case $action in
        "diagnose")
            check_docker_health || exit 1
            check_container_health
            check_sqlserver_logs
            test_sqlserver_connection
            ;;
        "clean")
            check_docker_health || exit 1
            cleanup_containers
            ;;
        "restart")
            check_docker_health || exit 1
            cleanup_containers
            restart_with_logs
            ;;
        "logs")
            echo "?? Container logs:"
            docker-compose logs
            ;;
        "sqlserver-logs")
            check_sqlserver_logs
            ;;
        "test-connection")
            test_sqlserver_connection
            ;;
        *)
            echo "Usage: $0 [diagnose|clean|restart|logs|sqlserver-logs|test-connection]"
            echo ""
            echo "Commands:"
            echo "  diagnose         Run full diagnostics (default)"
            echo "  clean            Clean up containers and volumes"
            echo "  restart          Clean restart with logs"
            echo "  logs             Show all container logs"
            echo "  sqlserver-logs   Show SQL Server logs only"
            echo "  test-connection  Test SQL Server connection"
            ;;
    esac
}

main "$@"