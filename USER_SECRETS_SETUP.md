# Configura��o com User Secrets para Desenvolvimento

Para configurar a connection string do Service Bus de forma segura em desenvolvimento, use User Secrets:

## 1. Inicializar User Secrets (se ainda n�o foi feito)
```bash
dotnet user-secrets init --project src/FCG.Games.Presentation
```

## 2. Definir a Connection String
```bash
# Substitua YOUR_ACCESS_KEY pela sua chave real
dotnet user-secrets set "ServiceBus:ConnectionString" "Endpoint=sb://fcg-games-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOUR_ACCESS_KEY" --project src/FCG.Games.Presentation
```

## 3. Outras configura��es opcionais
```bash
# Configurar nome da fila (opcional, j� tem padr�o)
dotnet user-secrets set "ServiceBus:SalesQueueName" "sale-processing-queue" --project src/FCG.Games.Presentation

# Configurar connection string do banco (se necess�rio)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\\SQLEXPRESS;Database=fcg-games-dev;Trusted_Connection=True;TrustServerCertificate=True;" --project src/FCG.Games.Presentation
```

## 4. Verificar os secrets configurados
```bash
dotnet user-secrets list --project src/FCG.Games.Presentation
```

## 5. Remover um secret (se necess�rio)
```bash
dotnet user-secrets remove "ServiceBus:ConnectionString" --project src/FCG.Games.Presentation
```

## 6. Limpar todos os secrets
```bash
dotnet user-secrets clear --project src/FCG.Games.Presentation
```

## Nota sobre Docker
User Secrets n�o funcionam diretamente no Docker. Para Docker, use:
- Vari�veis de ambiente
- Docker secrets
- Arquivo appsettings.Docker.json (para desenvolvimento)
- Azure Key Vault (para produ��o)