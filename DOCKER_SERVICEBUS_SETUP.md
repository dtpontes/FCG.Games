# ?? Guia de Configuração Docker + Service Bus

## ?? Resumo

Sim, **você precisa configurar a connection string do Service Bus no Docker Compose**! Aqui está como fazer:

## ?? Configuração Rápida

### 1. **Configure o Azure Service Bus:**
```powershell
# Execute o script de configuração
.\fix-servicebus.ps1
```

### 2. **Configure o arquivo .env:**
```powershell
# O script fix-servicebus.ps1 já configura automaticamente
# Mas você pode verificar/editar manualmente:
notepad .env
```

### 3. **Execute com Docker:**
```powershell
# Script completo de configuração e execução
.\run-docker.ps1
```

## ?? Configuração Manual

### 1. **Arquivo .env:**
```bash
# Sua connection string real aqui:
SERVICEBUS_CONNECTION_STRING=Endpoint=sb://fcg-games-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SUA_CHAVE_AQUI
```

### 2. **docker-compose.yml** (já configurado):
```yaml
environment:
  - ServiceBus__ConnectionString=${SERVICEBUS_CONNECTION_STRING}
  - ServiceBus__SalesQueueName=sale-processing-queue
  - ServiceBus__MaxConcurrentCalls=5
  - ServiceBus__MessageTimeoutSeconds=300
```

### 3. **docker-compose.override.yml** (já configurado):
```yaml
environment:
  - ServiceBus__ConnectionString=${SERVICEBUS_CONNECTION_STRING}
  - ServiceBus__MaxConcurrentCalls=3  # Menos para dev
  - Logging__LogLevel__Azure.Messaging.ServiceBus=Debug
```

## ?? Diferenças entre Ambientes

| Ambiente | Connection String | Onde Configurar |
|----------|------------------|-----------------|
| **Docker** | Arquivo `.env` | `SERVICEBUS_CONNECTION_STRING` |
| **Development** | User Secrets | `dotnet user-secrets set` |
| **Production** | Azure Key Vault | Variáveis de ambiente |

## ? Verificação

### 1. **Verificar se o Docker está funcionando:**
```powershell
# Verificar containers
docker-compose ps

# Verificar logs
docker-compose logs fcg.games.presentation
```

### 2. **Testar endpoints:**
```bash
# Status do Service Bus
GET http://localhost:5000/api/servicebusmonitor/status

# Testar conexão
GET http://localhost:5000/api/servicebusmonitor/test-connection

# Health check
GET http://localhost:5000/health
```

### 3. **Testar processamento de vendas:**
```bash
# Criar mensagem de exemplo
GET http://localhost:5000/api/test/create-sample-sale

# Enviar para a fila
POST http://localhost:5000/api/test/send-sale-message
```

## ?? Troubleshooting

### ? **Erro: InvalidSignature**
- Verifique se a connection string está correta no arquivo `.env`
- Execute `.\fix-servicebus.ps1` para obter nova connection string

### ? **Container não inicia**
- Verifique se o arquivo `.env` existe
- Verifique se não há containers conflitantes: `docker-compose down`

### ? **Service Bus não funciona**
- Verifique se o namespace existe no Azure
- Verifique se a fila `sale-processing-queue` foi criada
- Verifique os logs: `docker-compose logs fcg.games.presentation`

## ?? Arquivos Importantes

- ? `docker-compose.yml` - Configuração principal
- ? `docker-compose.override.yml` - Configurações de desenvolvimento  
- ? `.env` - **Suas credenciais (não commitar!)**
- ? `.env.example` - Exemplo de configuração
- ? `.gitignore` - Protege credenciais
- ? `fix-servicebus.ps1` - Script de configuração automática
- ? `run-docker.ps1` - Script de execução completa

## ?? **Resposta Direta:**

**Sim, você precisa configurar a connection string do Service Bus no Docker Compose!**

**Forma mais fácil:**
1. Execute: `.\fix-servicebus.ps1`
2. Execute: `.\run-docker.ps1` 
3. Teste: `http://localhost:5000/api/servicebusmonitor/test-connection`

Pronto! ??