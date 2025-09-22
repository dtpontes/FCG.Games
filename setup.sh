#!/bin/bash

# FCG Games Microservice Setup Script

echo "=== FCG Games Microservice Setup ==="
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "? Docker is not running. Please start Docker and try again."
    exit 1
fi

echo "? Docker is running"

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "? docker-compose is not installed. Please install docker-compose and try again."
    exit 1
fi

echo "? docker-compose is available"

# Function to show usage
show_usage() {
    echo ""
    echo "Usage: $0 [COMMAND]"
    echo ""
    echo "Commands:"
    echo "  dev       Start development environment"
    echo "  prod      Start production environment"
    echo "  stop      Stop all services"
    echo "  logs      Show service logs"
    echo "  health    Check service health"
    echo "  clean     Remove all containers and volumes"
    echo "  rebuild   Rebuild and restart services"
    echo ""
}

# Parse command line arguments
COMMAND=${1:-dev}

case $COMMAND in
    "dev")
        echo ""
        echo "?? Starting FCG Games Microservice in Development mode..."
        docker-compose up -d
        echo ""
        echo "? Services started successfully!"
        echo ""
        echo "?? Available endpoints:"
        echo "   • Games API (HTTP):  http://localhost:5001"
        echo "   • Games API (HTTPS): https://localhost:5002"
        echo "   • Swagger UI:         http://localhost:5001/swagger"
        echo "   • Health Check:       http://localhost:5001/health"
        echo "   • GraphQL:           http://localhost:5001/graphql"
        echo ""
        echo "?? View logs: $0 logs"
        echo "?? Stop services: $0 stop"
        ;;
    "prod")
        echo ""
        echo "?? Starting FCG Games Microservice in Production mode..."
        docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
        echo ""
        echo "? Production services started successfully!"
        ;;
    "stop")
        echo ""
        echo "?? Stopping all services..."
        docker-compose down
        echo "? All services stopped"
        ;;
    "logs")
        echo ""
        echo "?? Showing service logs (Press Ctrl+C to exit)..."
        docker-compose logs -f games-service
        ;;
    "health")
        echo ""
        echo "?? Checking service health..."
        echo ""
        echo "Games Service Health:"
        curl -s http://localhost:5001/health | jq '.' 2>/dev/null || curl -s http://localhost:5001/health
        echo ""
        echo ""
        echo "Container Status:"
        docker-compose ps
        ;;
    "clean")
        echo ""
        echo "?? Cleaning up all containers and volumes..."
        docker-compose down -v
        docker system prune -f
        echo "? Cleanup completed"
        ;;
    "rebuild")
        echo ""
        echo "?? Rebuilding and restarting services..."
        docker-compose down
        docker-compose build --no-cache
        docker-compose up -d
        echo "? Services rebuilt and restarted"
        ;;
    *)
        echo "? Unknown command: $COMMAND"
        show_usage
        exit 1
        ;;
esac