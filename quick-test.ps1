# Script Rápido para Testar a Conexão Service Bus

Write-Host "=== Teste Rápido - Service Bus Configurado ===" -ForegroundColor Green

# Verificar se arquivo .env existe
if (-not (Test-Path ".env")) {
    Write-Host "? Arquivo .env não encontrado!" -ForegroundColor Red
    exit 1
}

# Verificar connection string
$envContent = Get-Content ".env" -Raw
if ($envContent -match "SERVICEBUS_CONNECTION_STRING=.*83uilbws19wJIBUXcNOqNaDylJ9oFiDEv") {
    Write-Host "? Connection string configurada corretamente no .env" -ForegroundColor Green
} else {
    Write-Host "? Connection string não encontrada ou incorreta no .env" -ForegroundColor Red
    exit 1
}

# Executar Docker
Write-Host "`nIniciando containers..." -ForegroundColor Yellow
docker-compose up --build -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Containers iniciados!" -ForegroundColor Green
    
    # Aguardar um pouco
    Write-Host "`nAguardando inicialização (15 segundos)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 15
    
    # Testar endpoints
    Write-Host "`nTestando endpoints..." -ForegroundColor Yellow
    
    $baseUrl = "http://localhost:5000"
    
    # Testar Service Bus status
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/servicebusmonitor/status" -Method Get -TimeoutSec 10
        Write-Host "? Service Bus Status: OK" -ForegroundColor Green
        Write-Host "   Configurado: $($response.connectionStringConfigured)" -ForegroundColor Cyan
        Write-Host "   Fila: $($response.queueName)" -ForegroundColor Cyan
    } catch {
        Write-Host "? Erro ao testar status: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Testar conexão
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/servicebusmonitor/test-connection" -Method Get -TimeoutSec 15
        Write-Host "? Teste de Conexão: SUCESSO!" -ForegroundColor Green
        Write-Host "   Status: $($response.status)" -ForegroundColor Cyan
        Write-Host "   Mensagem: $($response.message)" -ForegroundColor Cyan
    } catch {
        Write-Host "? Erro no teste de conexão: $($_.Exception.Message)" -ForegroundColor Red
        
        # Tentar obter detalhes do erro
        try {
            $errorResponse = $_.Exception.Response
            if ($errorResponse) {
                $reader = [System.IO.StreamReader]::new($errorResponse.GetResponseStream())
                $errorDetails = $reader.ReadToEnd()
                $errorObj = $errorDetails | ConvertFrom-Json
                Write-Host "   Detalhes: $($errorObj.details)" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "   Sem detalhes adicionais do erro" -ForegroundColor Yellow
        }
    }
    
    Write-Host "`n?? Aplicação rodando em: http://localhost:5000" -ForegroundColor Cyan
    Write-Host "?? Swagger: http://localhost:5000/swagger" -ForegroundColor Cyan
    Write-Host "?? Service Bus Status: http://localhost:5000/api/servicebusmonitor/status" -ForegroundColor Cyan
    Write-Host "?? Test Connection: http://localhost:5000/api/servicebusmonitor/test-connection" -ForegroundColor Cyan
    
} else {
    Write-Host "? Erro ao iniciar containers" -ForegroundColor Red
    docker-compose logs
}

Write-Host "`n?? Para ver logs em tempo real: docker-compose logs -f fcg.games.presentation" -ForegroundColor Yellow