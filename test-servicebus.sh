#!/bin/bash

# Script Bash para Testar Service Bus
# Configurações
BASE_URL="https://localhost:5001"  # ou http://localhost:5000
GAME_ID=1
QUANTITY=2

echo "=== FCG Games - Teste do Service Bus ==="

# 1. Verificar status do Service Bus
echo
echo "1. Verificando status do Service Bus..."
STATUS_RESPONSE=$(curl -s -k "$BASE_URL/api/servicebusmonitor/status")
if [ $? -eq 0 ]; then
    echo "? Service Bus Status:"
    echo "$STATUS_RESPONSE" | jq '.'
else
    echo "? Erro ao verificar status"
    exit 1
fi

# 2. Verificar se há estoque disponível
echo
echo "2. Verificando estoque disponível..."
STOCK_RESPONSE=$(curl -s -k "$BASE_URL/api/stock/$GAME_ID")
if [ $? -eq 0 ]; then
    echo "? Estoque atual:"
    echo "$STOCK_RESPONSE" | jq '.'
    
    CURRENT_QUANTITY=$(echo "$STOCK_RESPONSE" | jq -r '.quantity')
    if [ "$CURRENT_QUANTITY" -lt "$QUANTITY" ]; then
        echo "??  Estoque insuficiente! Disponível: $CURRENT_QUANTITY, Solicitado: $QUANTITY"
        QUANTITY=1
        echo "   Ajustando quantidade para: $QUANTITY"
    fi
else
    echo "? Erro ao verificar estoque"
    echo "   Criando estoque de teste..."
    
    # Tentar criar estoque de teste
    ADD_STOCK_REQUEST='{"gameId": '$GAME_ID', "quantity": 10}'
    ADD_STOCK_RESPONSE=$(curl -s -k -X POST "$BASE_URL/api/stock/add" \
        -H "Content-Type: application/json" \
        -d "$ADD_STOCK_REQUEST")
    
    if [ $? -eq 0 ]; then
        echo "? Estoque criado com sucesso!"
    else
        echo "? Erro ao criar estoque"
        exit 1
    fi
fi

# 3. Criar mensagem de exemplo
echo
echo "3. Criando mensagem de exemplo..."
SAMPLE_RESPONSE=$(curl -s -k "$BASE_URL/api/test/create-sample-sale?gameId=$GAME_ID&quantity=$QUANTITY")
if [ $? -eq 0 ]; then
    echo "? Mensagem de exemplo criada:"
    echo "$SAMPLE_RESPONSE" | jq '.'
    SALE_MESSAGE=$(echo "$SAMPLE_RESPONSE" | jq '.sampleMessage')
else
    echo "? Erro ao criar mensagem de exemplo"
    exit 1
fi

# 4. Enviar mensagem para a fila
echo
echo "4. Enviando mensagem para a fila Service Bus..."
SEND_RESPONSE=$(curl -s -k -X POST "$BASE_URL/api/test/send-sale-message" \
    -H "Content-Type: application/json" \
    -d "$SALE_MESSAGE")

if [ $? -eq 0 ]; then
    echo "? Mensagem enviada com sucesso:"
    echo "$SEND_RESPONSE" | jq '.'
    TRANSACTION_ID=$(echo "$SEND_RESPONSE" | jq -r '.transactionId')
    echo "   Transaction ID: $TRANSACTION_ID"
else
    echo "? Erro ao enviar mensagem"
    exit 1
fi

# 5. Aguardar processamento
echo
echo "5. Aguardando processamento (10 segundos)..."
sleep 10

# 6. Verificar estoque após processamento
echo
echo "6. Verificando estoque após processamento..."
FINAL_STOCK_RESPONSE=$(curl -s -k "$BASE_URL/api/stock/$GAME_ID")
if [ $? -eq 0 ]; then
    echo "? Estoque após processamento:"
    echo "$FINAL_STOCK_RESPONSE" | jq '.'
    
    FINAL_QUANTITY=$(echo "$FINAL_STOCK_RESPONSE" | jq -r '.quantity')
    PROCESSED_QUANTITY=$((CURRENT_QUANTITY - FINAL_QUANTITY))
    
    if [ "$PROCESSED_QUANTITY" -eq "$QUANTITY" ]; then
        echo "? Venda processada com sucesso! Quantidade debitada: $PROCESSED_QUANTITY"
    else
        echo "??  Quantidade processada: $PROCESSED_QUANTITY (esperado: $QUANTITY)"
    fi
else
    echo "? Erro ao verificar estoque final"
fi

echo
echo "=== Teste Concluído ==="
echo "Para monitorar logs em tempo real, execute: docker-compose logs -f fcg.games.presentation"