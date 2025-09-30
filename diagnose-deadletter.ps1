# Script para Diagnosticar Mensagens Dead Letter

Write-Host "=== Diagnóstico de Mensagens Dead Letter ===" -ForegroundColor Green

$baseUrl = "http://localhost:5000"

# 1. Verificar dead letter messages
Write-Host "`n1. Verificando mensagens na Dead Letter Queue..." -ForegroundColor Yellow
try {
    $deadLetterResponse = Invoke-RestMethod -Uri "$baseUrl/api/servicebusmonitor/dead-letter-messages?maxMessages=5" -Method Get -TimeoutSec 15
    Write-Host "? Dead Letter Messages encontradas:" -ForegroundColor Green
    Write-Host "   Quantidade: $($deadLetterResponse.deadLetterMessagesCount)" -ForegroundColor Cyan
    
    if ($deadLetterResponse.messages -and $deadLetterResponse.messages.Count -gt 0) {
        Write-Host "`n?? Detalhes das mensagens:" -ForegroundColor Yellow
        foreach ($message in $deadLetterResponse.messages) {
            Write-Host "   Message ID: $($message.messageId)" -ForegroundColor White
            Write-Host "   Reason: $($message.deadLetterReason)" -ForegroundColor Red
            Write-Host "   Description: $($message.deadLetterErrorDescription)" -ForegroundColor Red
            Write-Host "   Body: $($message.body)" -ForegroundColor Gray
            Write-Host "   ---" -ForegroundColor DarkGray
        }
    }
} catch {
    Write-Host "? Erro ao verificar dead letter: $($_.Exception.Message)" -ForegroundColor Red
}

# 2. Enviar uma mensagem de teste e monitorar
Write-Host "`n2. Enviando mensagem de teste..." -ForegroundColor Yellow
try {
    # Primeiro criar sample
    $sampleResponse = Invoke-RestMethod -Uri "$baseUrl/api/test/create-sample-sale?gameId=1&quantity=1" -Method Get -TimeoutSec 15
    Write-Host "? Sample criado:" -ForegroundColor Green
    Write-Host "   GameId: $($sampleResponse.sampleMessage.gameId)" -ForegroundColor Cyan
    Write-Host "   TransactionId: $($sampleResponse.sampleMessage.transactionId)" -ForegroundColor Cyan
    
    # Enviar mensagem
    Write-Host "`n   Enviando para fila..." -ForegroundColor Yellow
    $sendResponse = Invoke-RestMethod -Uri "$baseUrl/api/test/send-sale-message" -Method Post -Body ($sampleResponse.sampleMessage | ConvertTo-Json) -ContentType "application/json" -TimeoutSec 15
    Write-Host "? Mensagem enviada:" -ForegroundColor Green
    Write-Host "   TransactionId: $($sendResponse.transactionId)" -ForegroundColor Cyan
    
    # Aguardar processamento
    Write-Host "`n   Aguardando processamento (15 segundos)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 15
    
    # Verificar novamente dead letter
    Write-Host "`n   Verificando se foi para dead letter..." -ForegroundColor Yellow
    $deadLetterResponse2 = Invoke-RestMethod -Uri "$baseUrl/api/servicebusmonitor/dead-letter-messages?maxMessages=10" -Method Get -TimeoutSec 15
    
    if ($deadLetterResponse2.deadLetterMessagesCount -gt $deadLetterResponse.deadLetterMessagesCount) {
        Write-Host "? Nova mensagem foi para Dead Letter!" -ForegroundColor Red
        $newMessages = $deadLetterResponse2.messages | Where-Object { $_.messageId -notin $deadLetterResponse.messages.messageId }
        foreach ($newMessage in $newMessages) {
            Write-Host "   ?? Nova mensagem dead letter:" -ForegroundColor Red
            Write-Host "      Message ID: $($newMessage.messageId)" -ForegroundColor White
            Write-Host "      Reason: $($newMessage.deadLetterReason)" -ForegroundColor Red
            Write-Host "      Description: $($newMessage.deadLetterErrorDescription)" -ForegroundColor Red
            Write-Host "      Body: $($newMessage.body)" -ForegroundColor Gray
        }
    } else {
        Write-Host "? Mensagem processada com sucesso (não foi para dead letter)" -ForegroundColor Green
    }
    
} catch {
    Write-Host "? Erro no teste: $($_.Exception.Message)" -ForegroundColor Red
}

# 3. Verificar logs da aplicação
Write-Host "`n3. Para ver logs detalhados execute:" -ForegroundColor Yellow
Write-Host "   docker-compose logs -f fcg.games.presentation" -ForegroundColor Cyan

# 4. Possíveis causas
Write-Host "`n?? Possíveis causas de Dead Letter:" -ForegroundColor Yellow
Write-Host "   1. Erro de deserialização JSON" -ForegroundColor White
Write-Host "   2. Jogo não encontrado no estoque" -ForegroundColor White
Write-Host "   3. Estoque insuficiente" -ForegroundColor White
Write-Host "   4. Validação de dados falhou" -ForegroundColor White
Write-Host "   5. Erro de conexão com banco de dados" -ForegroundColor White

Write-Host "`n?? Soluções recomendadas:" -ForegroundColor Yellow
Write-Host "   • Verificar se existe Game ID 1 no banco" -ForegroundColor Cyan
Write-Host "   • Verificar se há estoque para Game ID 1" -ForegroundColor Cyan
Write-Host "   • Verificar logs da aplicação" -ForegroundColor Cyan
Write-Host "   • Testar com /api/stock/1 para ver se jogo existe" -ForegroundColor Cyan

Write-Host "`n=== Diagnóstico Concluído ===" -ForegroundColor Green