# Script para testar Service Bus - Sem arquivo .env

Write-Host "=== Teste Service Bus - Configuração Direta ===" -ForegroundColor Green

# Verificar se docker-compose.yml existe
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "? docker-compose.yml não encontrado!" -ForegroundColor Red
    exit 1
}

Write-Host "? docker-compose.yml encontrado" -ForegroundColor Green

# Parar containers existentes
Write-Host "`nParando containers existentes..." -ForegroundColor Yellow
docker-compose down

# Iniciar containers
Write-Host "`nIniciando containers..." -ForegroundColor Yellow
docker-compose up --build -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Containers iniciados!" -ForegroundColor Green
    
    # Aguardar inicialização
    Write-Host "`nAguardando inicialização (20 segundos)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 20
    
    # Testar Service Bus Status
    Write-Host "`nTestando Service Bus Status..." -ForegroundColor Yellow
    try {
        $statusResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/servicebusmonitor/status" -Method Get -TimeoutSec 15
        Write-Host "? Service Bus Status: OK" -ForegroundColor Green
        Write-Host "   Configurado: $($statusResponse.connectionStringConfigured)" -ForegroundColor Cyan
        Write-Host "   Fila: $($statusResponse.queueName)" -ForegroundColor Cyan
        Write-Host "   Connection String: $($statusResponse.connectionStringMasked)" -ForegroundColor Cyan
    } catch {
        Write-Host "? Erro no Status: $($_.Exception.Message)" -ForegroundColor Red
        return
    }
    
    # Testar Conexão
    Write-Host "`nTestando Conexão Service Bus..." -ForegroundColor Yellow
    try {
        $connectionResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/servicebusmonitor/test-connection" -Method Get -TimeoutSec 15
        Write-Host "? Conexão: $($connectionResponse.status)" -ForegroundColor Green
        Write-Host "   Mensagem: $($connectionResponse.message)" -ForegroundColor Cyan
        Write-Host "   Fila: $($connectionResponse.queueName)" -ForegroundColor Cyan
    } catch {
        Write-Host "? Erro na Conexão: $($_.Exception.Message)" -ForegroundColor Red
        return
    }
    
    # Testar Create Sample Sale
    Write-Host "`nTestando Create Sample Sale..." -ForegroundColor Yellow
    try {
        $sampleResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/test/create-sample-sale?gameId=1&quantity=1" -Method Get -TimeoutSec 15
        Write-Host "? Sample Sale criado:" -ForegroundColor Green
        Write-Host "   Transaction ID: $($sampleResponse.sampleMessage.transactionId)" -ForegroundColor Cyan
        Write-Host "   Game ID: $($sampleResponse.sampleMessage.gameId)" -ForegroundColor Cyan
        Write-Host "   Quantidade: $($sampleResponse.sampleMessage.quantity)" -ForegroundColor Cyan
        Write-Host "   Valor: $($sampleResponse.sampleMessage.totalAmount)" -ForegroundColor Cyan
        
        # Salvar mensagem para envio posterior
        $global:saleMessage = $sampleResponse.sampleMessage
        
    } catch {
        Write-Host "? Erro no Create Sample: $($_.Exception.Message)" -ForegroundColor Red
        
        # Tentar obter detalhes do erro
        try {
            $errorResponse = $_.Exception.Response
            if ($errorResponse) {
                $reader = [System.IO.StreamReader]::new($errorResponse.GetResponseStream())
                $errorDetails = $reader.ReadToEnd()
                Write-Host "   Detalhes: $errorDetails" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "   Sem detalhes adicionais" -ForegroundColor Yellow
        }
    }
    
    # Testar Envio para Fila (se sample foi criado)
    if ($global:saleMessage) {
        Write-Host "`nTestando Envio para Fila..." -ForegroundColor Yellow
        try {
            $sendResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/test/send-sale-message" -Method Post -Body ($global:saleMessage | ConvertTo-Json) -ContentType "application/json" -TimeoutSec 15
            Write-Host "? Mensagem enviada:" -ForegroundColor Green
            Write-Host "   Transaction ID: $($sendResponse.transactionId)" -ForegroundColor Cyan
            Write-Host "   Queue: $($sendResponse.queueName)" -ForegroundColor Cyan
            
            Write-Host "`nAguardando processamento (10 segundos)..." -ForegroundColor Yellow
            Start-Sleep -Seconds 10
            
            Write-Host "? Teste completo realizado com sucesso!" -ForegroundColor Green
            
        } catch {
            Write-Host "? Erro no Envio: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host "`n=== Resumo dos Testes ===" -ForegroundColor Green
    Write-Host "?? Aplicação: http://localhost:5000" -ForegroundColor Cyan
    Write-Host "?? Swagger: http://localhost:5000/swagger" -ForegroundColor Cyan
    Write-Host "?? Health: http://localhost:5000/health" -ForegroundColor Cyan
    Write-Host "?? Logs: docker-compose logs -f fcg.games.presentation" -ForegroundColor Cyan
    
} else {
    Write-Host "? Erro ao iniciar containers" -ForegroundColor Red
    docker-compose logs
}

Write-Host "`n? Configuração direta nos arquivos appsettings.json e docker-compose.yml" -ForegroundColor Green
Write-Host "?? Não há dependência de arquivo .env" -ForegroundColor Green