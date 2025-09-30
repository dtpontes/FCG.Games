# ?? Guia de Configura��o Docker + Service Bus

## ?? Resumo

Sim, **voc� precisa configurar a connection string do Service Bus no Docker Compose**! Aqui est� como fazer:

## ?? Configura��o R�pida

### 1. **Configure o Azure Service Bus:**
```powershell
# Execute o script de configura��o
.\fix-servicebus.ps1
```

### 2. **Configure o arquivo .env:**
```powershell
# O script fix-servicebus.ps1 j� configura automaticamente
# Mas voc� pode verificar/editar manualmente:
notepad .env
```

### 3. **Execute com Docker:**
```powershell
# Script completo de configura��o e execu��o
.\run-docker.ps1
```

## ?? Configura��o Manual

### 1. **Arquivo .env:**
```bash
# Sua connection string real aqui:
SERVICEBUS_CONNECTION_STRING=Endpoint=sb://fcg-games-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SUA_CHAVE_AQUI
```

### 2. **docker-compose.yml** (j� configurado):
```yaml
environment:
  - ServiceBus__ConnectionString=${SERVICEBUS_CONNECTION_STRING}
  - ServiceBus__SalesQueueName=sale-processing-queue
  - ServiceBus__MaxConcurrentCalls=5
  - ServiceBus__MessageTimeoutSeconds=300
```

### 3. **docker-compose.override.yml** (j� configurado):
```yaml
environment:
  - ServiceBus__ConnectionString=${SERVICEBUS_CONNECTION_STRING}
  - ServiceBus__MaxConcurrentCalls=3  # Menos para dev
  - Logging__LogLevel__Azure.Messaging.ServiceBus=Debug
```

## ?? Diferen�as entre Ambientes

| Ambiente | Connection String | Onde Configurar |
|----------|------------------|-----------------|
| **Docker** | Arquivo `.env` | `SERVICEBUS_CONNECTION_STRING` |
| **Development** | User Secrets | `dotnet user-secrets set` |
| **Production** | Azure Key Vault | Vari�veis de ambiente |

## ? Verifica��o

### 1. **Verificar se o Docker est� funcionando:**
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

# Testar conex�o
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
- Verifique se a connection string est� correta no arquivo `.env`
- Execute `.\fix-servicebus.ps1` para obter nova connection string

### ? **Container n�o inicia**
- Verifique se o arquivo `.env` existe
- Verifique se n�o h� containers conflitantes: `docker-compose down`

### ? **Service Bus n�o funciona**
- Verifique se o namespace existe no Azure
- Verifique se a fila `sale-processing-queue` foi criada
- Verifique os logs: `docker-compose logs fcg.games.presentation`

## ?? Arquivos Importantes

- ? `docker-compose.yml` - Configura��o principal
- ? `docker-compose.override.yml` - Configura��es de desenvolvimento  
- ? `.env` - **Suas credenciais (n�o commitar!)**
- ? `.env.example` - Exemplo de configura��o
- ? `.gitignore` - Protege credenciais
- ? `fix-servicebus.ps1` - Script de configura��o autom�tica
- ? `run-docker.ps1` - Script de execu��o completa

## ?? **Resposta Direta:**

**Sim, voc� precisa configurar a connection string do Service Bus no Docker Compose!**

**Forma mais f�cil:**
1. Execute: `.\fix-servicebus.ps1`
2. Execute: `.\run-docker.ps1` 
3. Teste: `http://localhost:5000/api/servicebusmonitor/test-connection`

Pronto! ??