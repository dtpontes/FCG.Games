# Script para configurar e executar Docker com Service Bus

Write-Host "=== Configuração Docker + Service Bus ===" -ForegroundColor Green

# 1. Verificar se o arquivo .env existe
Write-Host "`n1. Verificando arquivo .env..." -ForegroundColor Yellow
if (-not (Test-Path ".env")) {
    Write-Host "? Arquivo .env não encontrado!" -ForegroundColor Red
    Write-Host "Por favor, configure a connection string no arquivo .env" -ForegroundColor Yellow
    exit 1
}

# 2. Verificar se a connection string está configurada
Write-Host "`n2. Verificando configuração Service Bus..." -ForegroundColor Yellow
$envContent = Get-Content ".env" -Raw
if ($envContent -match "SERVICEBUS_CONNECTION_STRING=.*your-real-access-key-here") {
    Write-Host "??  Connection string ainda não foi configurada!" -ForegroundColor Yellow
    Write-Host "Execute: .\fix-servicebus.ps1 primeiro para obter a connection string" -ForegroundColor Cyan
    
    $response = Read-Host "Deseja continuar mesmo assim? (y/N)"
    if ($response -ne "y" -and $response -ne "Y") {
        exit 1
    }
} else {
    Write-Host "? Connection string configurada" -ForegroundColor Green
}

# 3. Parar containers existentes
Write-Host "`n3. Parando containers existentes..." -ForegroundColor Yellow
docker-compose down

# 4. Limpar volumes (opcional)
$cleanVolumes = Read-Host "Deseja limpar volumes do banco de dados? (y/N)"
if ($cleanVolumes -eq "y" -or $cleanVolumes -eq "Y") {
    Write-Host "Removendo volumes..." -ForegroundColor Yellow
    docker-compose down -v
}

# 5. Build e iniciar containers
Write-Host "`n4. Construindo e iniciando containers..." -ForegroundColor Yellow
docker-compose up --build -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Containers iniciados com sucesso!" -ForegroundColor Green
} else {
    Write-Host "? Erro ao iniciar containers" -ForegroundColor Red
    exit 1
}

# 6. Aguardar inicialização
Write-Host "`n5. Aguardando inicialização dos serviços..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# 7. Verificar status dos containers
Write-Host "`n6. Verificando status dos containers..." -ForegroundColor Yellow
docker-compose ps

# 8. Testar endpoints
Write-Host "`n7. Testando endpoints..." -ForegroundColor Yellow

$baseUrl = "http://localhost:5000"

# Testar health check
try {
    Write-Host "   Testando health check..." -ForegroundColor Cyan
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -TimeoutSec 10
    Write-Host "   ? Health check: OK" -ForegroundColor Green
} catch {
    Write-Host "   ??  Health check falhou: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Testar Service Bus monitor
try {
    Write-Host "   Testando Service Bus monitor..." -ForegroundColor Cyan
    $sbResponse = Invoke-RestMethod -Uri "$baseUrl/api/servicebusmonitor/status" -Method Get -TimeoutSec 10
    Write-Host "   ? Service Bus monitor: OK" -ForegroundColor Green
} catch {
    Write-Host "   ??  Service Bus monitor falhou: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n=== Configuração Concluída ===" -ForegroundColor Green
Write-Host "?? Aplicação rodando em: http://localhost:5000" -ForegroundColor Cyan
Write-Host "?? Health Check: http://localhost:5000/health" -ForegroundColor Cyan
Write-Host "?? Service Bus Status: http://localhost:5000/api/servicebusmonitor/status" -ForegroundColor Cyan
Write-Host "?? Teste conexão SB: http://localhost:5000/api/servicebusmonitor/test-connection" -ForegroundColor Cyan
Write-Host "?? Swagger: http://localhost:5000/swagger" -ForegroundColor Cyan

Write-Host "`n?? Comandos úteis:" -ForegroundColor Yellow
Write-Host "   Ver logs: docker-compose logs -f" -ForegroundColor Cyan
Write-Host "   Parar: docker-compose down" -ForegroundColor Cyan
Write-Host "   Reiniciar: docker-compose restart" -ForegroundColor Cyan