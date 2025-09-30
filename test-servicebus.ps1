# Script PowerShell para Testar Service Bus - Versão Atualizada

# Configurações
$baseUrl = "https://localhost:5001"  # ou http://localhost:5000
$gameId = 1
$quantity = 2

# Ignorar certificados SSL em desenvolvimento
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

Write-Host "=== FCG Games - Teste do Service Bus (Atualizado) ===" -ForegroundColor Green

# 1. Verificar status do Service Bus
Write-Host "`n1. Verificando status do Service Bus..." -ForegroundColor Yellow
try {
    $statusResponse = Invoke-RestMethod -Uri "$baseUrl/api/servicebusmonitor/status" -Method Get
    Write-Host "? Service Bus Status:" -ForegroundColor Green
    $statusResponse | ConvertTo-Json -Depth 3 | Write-Host
} catch {
    Write-Host "? Erro ao verificar status: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Tentando com HTTP..." -ForegroundColor Yellow
    $baseUrl = "http://localhost:5000"
    try {
        $statusResponse = Invoke-RestMethod -Uri "$baseUrl/api/servicebusmonitor/status" -Method Get
        Write-Host "? Service Bus Status (HTTP):" -ForegroundColor Green
        $statusResponse | ConvertTo-Json -Depth 3 | Write-Host
    } catch {
        Write-Host "? Erro também com HTTP: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   Verifique se a aplicação está rodando" -ForegroundColor Yellow
        exit 1
    }
}

# 2. Testar conexão com Service Bus
Write-Host "`n2. Testando conexão com Service Bus..." -ForegroundColor Yellow
try {
    $connectionTest = Invoke-RestMethod -Uri "$baseUrl/api/servicebusmonitor/test-connection" -Method Get
    Write-Host "? Conexão testada com sucesso:" -ForegroundColor Green
    $connectionTest | ConvertTo-Json -Depth 3 | Write-Host
} catch {
    Write-Host "? Erro ao testar conexão: $($_.Exception.Message)" -ForegroundColor Red
    
    # Tentar obter mais detalhes do erro
    try {
        $errorResponse = $_.Exception.Response
        if ($errorResponse) {
            $reader = [System.IO.StreamReader]::new($errorResponse.GetResponseStream())
            $errorDetails = $reader.ReadToEnd()
            Write-Host "   Detalhes do erro:" -ForegroundColor Yellow
            $errorDetails | ConvertFrom-Json | ConvertTo-Json -Depth 3 | Write-Host
        }
    } catch {
        Write-Host "   Não foi possível obter detalhes do erro" -ForegroundColor Yellow
    }
    
    Write-Host "`n??  Erro de conexão detectado. Verifique:" -ForegroundColor Yellow
    Write-Host "   1. Se a connection string está configurada corretamente" -ForegroundColor Cyan
    Write-Host "   2. Se o namespace Azure Service Bus existe" -ForegroundColor Cyan
    Write-Host "   3. Execute: .\fix-servicebus.ps1 para configurar automaticamente" -ForegroundColor Cyan
}

# 3. Verificar se há estoque disponível
Write-Host "`n3. Verificando estoque disponível..." -ForegroundColor Yellow
try {
    $stockResponse = Invoke-RestMethod -Uri "$baseUrl/api/stock/$gameId" -Method Get
    Write-Host "? Estoque atual:" -ForegroundColor Green
    Write-Host "   Game ID: $($stockResponse.gameId)" -ForegroundColor Cyan
    Write-Host "   Nome: $($stockResponse.gameName)" -ForegroundColor Cyan
    Write-Host "   Quantidade: $($stockResponse.quantity)" -ForegroundColor Cyan
    
    if ($stockResponse.quantity -lt $quantity) {
        Write-Host "??  Estoque insuficiente! Disponível: $($stockResponse.quantity), Solicitado: $quantity" -ForegroundColor Yellow
        $quantity = [Math]::Min($stockResponse.quantity, 1)
        Write-Host "   Ajustando quantidade para: $quantity" -ForegroundColor Yellow
    }
} catch {
    Write-Host "? Erro ao verificar estoque: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Criando estoque de teste..." -ForegroundColor Yellow
    
    # Tentar criar estoque de teste
    $addStockRequest = @{
        gameId = $gameId
        quantity = 10
    }
    
    try {
        $addStockResponse = Invoke-RestMethod -Uri "$baseUrl/api/stock/add" -Method Post -Body ($addStockRequest | ConvertTo-Json) -ContentType "application/json"
        Write-Host "? Estoque criado com sucesso!" -ForegroundColor Green
    } catch {
        Write-Host "? Erro ao criar estoque: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   Continuando mesmo assim..." -ForegroundColor Yellow
    }
}

# 4. Criar mensagem de exemplo
Write-Host "`n4. Criando mensagem de exemplo..." -ForegroundColor Yellow
try {
    $sampleResponse = Invoke-RestMethod -Uri "$baseUrl/api/test/create-sample-sale?gameId=$gameId&quantity=$quantity" -Method Get
    Write-Host "? Mensagem de exemplo criada:" -ForegroundColor Green
    $saleMessage = $sampleResponse.sampleMessage
    $saleMessage | ConvertTo-Json -Depth 3 | Write-Host
} catch {
    Write-Host "? Erro ao criar mensagem de exemplo: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Criando mensagem manual..." -ForegroundColor Yellow
    
    $saleMessage = @{
        transactionId = [System.Guid]::NewGuid().ToString()
        gameId = $gameId
        quantity = $quantity
        saleDateTime = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        userId = "test-user-123"
        totalAmount = 59.99 * $quantity
        sourceService = "FCG.Sales.Test"
    }
    Write-Host "? Mensagem manual criada" -ForegroundColor Green
}

# 5. Tentar enviar mensagem de teste para Service Bus (se conexão funcionou)
if ($connectionTest -and $connectionTest.Status -eq "Success") {
    Write-Host "`n5. Enviando mensagem para a fila Service Bus..." -ForegroundColor Yellow
    try {
        $sendResponse = Invoke-RestMethod -Uri "$baseUrl/api/test/send-sale-message" -Method Post -Body ($saleMessage | ConvertTo-Json) -ContentType "application/json"
        Write-Host "? Mensagem enviada com sucesso:" -ForegroundColor Green
        Write-Host "   Transaction ID: $($sendResponse.transactionId)" -ForegroundColor Cyan
        Write-Host "   Queue: $($sendResponse.queueName)" -ForegroundColor Cyan
        
        # 6. Aguardar processamento
        Write-Host "`n6. Aguardando processamento (10 segundos)..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
        
        # 7. Verificar estoque após processamento
        Write-Host "`n7. Verificando estoque após processamento..." -ForegroundColor Yellow
        try {
            $finalStockResponse = Invoke-RestMethod -Uri "$baseUrl/api/stock/$gameId" -Method Get
            Write-Host "? Estoque após processamento:" -ForegroundColor Green
            Write-Host "   Quantidade: $($finalStockResponse.quantity)" -ForegroundColor Cyan
            
            if ($stockResponse) {
                $processedQuantity = $stockResponse.quantity - $finalStockResponse.quantity
                if ($processedQuantity -eq $quantity) {
                    Write-Host "? Venda processada com sucesso! Quantidade debitada: $processedQuantity" -ForegroundColor Green
                } else {
                    Write-Host "??  Quantidade processada: $processedQuantity (esperado: $quantity)" -ForegroundColor Yellow
                }
            }
        } catch {
            Write-Host "? Erro ao verificar estoque final: $($_.Exception.Message)" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "? Erro ao enviar mensagem: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "`n5. ??  Pulando teste de envio - conexão Service Bus falhou" -ForegroundColor Yellow
    Write-Host "   Configure o Service Bus primeiro executando: .\fix-servicebus.ps1" -ForegroundColor Cyan
}

Write-Host "`n=== Teste Concluído ===" -ForegroundColor Green
Write-Host "?? Aplicação rodando em: $baseUrl" -ForegroundColor Cyan
Write-Host "?? Health Check: $baseUrl/health" -ForegroundColor Cyan
Write-Host "?? Service Bus Status: $baseUrl/api/servicebusmonitor/status" -ForegroundColor Cyan
Write-Host "?? Teste conexão SB: $baseUrl/api/servicebusmonitor/test-connection" -ForegroundColor Cyan
Write-Host "?? Swagger: $baseUrl/swagger" -ForegroundColor Cyan

if (-not ($connectionTest -and $connectionTest.Status -eq "Success")) {
    Write-Host "`n?? Para corrigir problemas do Service Bus:" -ForegroundColor Yellow
    Write-Host "   1. Execute: .\fix-servicebus.ps1" -ForegroundColor Cyan
    Write-Host "   2. Ou configure manualmente a connection string no arquivo .env" -ForegroundColor Cyan
}

Write-Host "`n?? Comandos úteis:" -ForegroundColor Yellow
Write-Host "   Ver logs: docker-compose logs -f fcg.games.presentation" -ForegroundColor Cyan
Write-Host "   Reiniciar: docker-compose restart" -ForegroundColor Cyan