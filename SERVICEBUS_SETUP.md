# Sistema de Processamento de Vendas com Service Bus

Este sistema processa vendas automaticamente através do Azure Service Bus, debitando do estoque quando uma venda é realizada.

## ??? Arquitetura

```
Microserviço de Vendas ? Azure Service Bus ? Microserviço de Games (Este projeto)
                            (Fila)                    ?
                                                Debita Estoque
```

## ?? Configuração

### 1. Azure Service Bus

Primeiro, configure o Service Bus no Azure:

```bash
# Criar resource group
az group create --name "fcg-games-rg" --location "East US"

# Criar Service Bus namespace
az servicebus namespace create \
  --resource-group "fcg-games-rg" \
  --name "fcg-games-servicebus" \
  --location "East US" \
  --sku Standard

# Criar fila
az servicebus queue create \
  --resource-group "fcg-games-rg" \
  --namespace-name "fcg-games-servicebus" \
  --name "sale-processing-queue" \
  --max-size 1024

# Obter connection string
az servicebus namespace authorization-rule keys list \
  --resource-group "fcg-games-rg" \
  --namespace-name "fcg-games-servicebus" \
  --name "RootManageSharedAccessKey" \
  --query primaryConnectionString --output tsv
```

### 2. Configuração do Projeto

Atualize a connection string no `appsettings.json`:

```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://fcg-games-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SUA_ACCESS_KEY_AQUI",
    "SalesQueueName": "sale-processing-queue",
    "MaxConcurrentCalls": 5,
    "MessageTimeoutSeconds": 300
  }
}
```

## ?? Como Funciona

### 1. Processamento Automático
- O `SaleProcessingBackgroundService` roda em background
- Conecta automaticamente na fila `sale-processing-queue`
- Processa mensagens de venda em tempo real

### 2. Fluxo de Processamento
1. Mensagem de venda chega na fila
2. Sistema deserializa a mensagem (`SaleMessageDto`)
3. Valida os dados da venda
4. Verifica se há estoque suficiente
5. Debita a quantidade vendida do estoque
6. Remove mensagem da fila (ou envia para dead letter em caso de erro)

### 3. Estrutura da Mensagem

```json
{
  "transactionId": "guid-unique",
  "gameId": 123,
  "quantity": 2,
  "saleDateTime": "2024-01-15T10:30:00Z",
  "userId": "user-456",
  "totalAmount": 119.98,
  "sourceService": "FCG.Sales"
}
```

## ?? Testando o Sistema

### 1. Verificar Status
```http
GET /api/servicebusmonitor/status
GET /api/servicebusmonitor/health
```

### 2. Criar Mensagem de Teste
```http
GET /api/test/create-sample-sale?gameId=1&quantity=2
```

### 3. Enviar Mensagem para a Fila
```http
POST /api/test/send-sale-message
Content-Type: application/json

{
  "gameId": 1,
  "quantity": 1,
  "userId": "test-user",
  "totalAmount": 59.99
}
```

### 4. Teste de Carga
```http
POST /api/test/send-multiple-sales?gameId=1&count=10
```

## ?? Monitoramento

### Logs Importantes
- `FCG.Games.Service.SaleProcessingService`: Processamento de vendas
- `FCG.Games.Presentation.Services.ServiceBusService`: Operações do Service Bus
- `Azure.Messaging.ServiceBus`: Logs internos do SDK

### Cenários de Erro
1. **Estoque Insuficiente**: Mensagem vai para dead letter
2. **Jogo não encontrado**: Mensagem vai para dead letter  
3. **Erro temporário**: Mensagem é reprocessada
4. **Erro de deserialização**: Mensagem vai para dead letter

## ?? Configurações Avançadas

### Desenvolvimento
```json
{
  "ServiceBus": {
    "MaxConcurrentCalls": 3,
    "MessageTimeoutSeconds": 180
  },
  "Logging": {
    "LogLevel": {
      "Azure.Messaging.ServiceBus": "Debug",
      "FCG.Games.Service.SaleProcessingService": "Debug"
    }
  }
}
```

### Produção
```json
{
  "ServiceBus": {
    "MaxConcurrentCalls": 10,
    "MessageTimeoutSeconds": 300
  }
}
```

## ?? Docker

O sistema está configurado para funcionar com Docker:

```bash
docker-compose up -d
```

A configuração específica do Docker está em `appsettings.Docker.json`.

## ?? Troubleshooting

### 1. Connection String Inválida
- Verifique se a connection string está correta
- Confirme se o namespace existe no Azure

### 2. Fila Não Encontrada
- Verifique se a fila `sale-processing-queue` existe
- Confirme o nome da fila na configuração

### 3. Mensagens Não Sendo Processadas
- Verifique os logs do `SaleProcessingBackgroundService`
- Confirme se o serviço está iniciando corretamente

### 4. Erros de Estoque
- Verifique se há jogos cadastrados no estoque
- Confirme se o `StockService` está funcionando

## ?? Performance

- **MaxConcurrentCalls**: Controla quantas mensagens são processadas simultaneamente
- **MessageTimeoutSeconds**: Tempo limite para processar uma mensagem
- Sistema suporta processamento em lote para alta performance