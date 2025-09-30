# ?? Configura��o Segura de Credenciais

## ?? **IMPORTANTE - N�o commitar credenciais!**

Este projeto requer configura��o de credenciais do Azure Service Bus. **NUNCA** commite credenciais reais no Git.

## ??? **Setup para Desenvolvimento Local:**

### **1. Copiar arquivo de exemplo:**
```bash
cp .env.example .env
```

### **2. Obter connection string do Azure:**
```bash
# Login no Azure
az login

# Obter connection string
az servicebus namespace authorization-rule keys list \
  --resource-group "fcg-games-rg" \
  --namespace-name "fcg-games-servicebus" \
  --name "RootManageSharedAccessKey" \
  --query primaryConnectionString --output tsv
```

### **3. Configurar arquivo .env:**
```bash
# Editar .env com sua connection string real
SERVICEBUS_CONNECTION_STRING=Endpoint=sb://fcg-games-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SUA_CHAVE_AQUI
```

### **4. Configurar User Secrets (Alternativo):**
```bash
cd src/FCG.Games.Presentation
dotnet user-secrets set "ServiceBus:ConnectionString" "SUA_CONNECTION_STRING_AQUI"
```

## ?? **Setup para Docker:**

### **1. Definir vari�vel de ambiente:**
```bash
# Windows
$env:SERVICEBUS_CONNECTION_STRING="SUA_CONNECTION_STRING_AQUI"

# Linux/Mac
export SERVICEBUS_CONNECTION_STRING="SUA_CONNECTION_STRING_AQUI"
```

### **2. Executar Docker:**
```bash
docker-compose up -d
```

## ?? **Setup para Produ��o:**

### **1. Azure Key Vault:**
- Armazenar credenciais no Azure Key Vault
- Configurar Managed Identity
- Usar Azure App Configuration

### **2. Vari�veis de Ambiente:**
- Configurar no Azure App Service
- Usar Azure DevOps Variables
- GitHub Secrets para CI/CD

## ?? **Boas Pr�ticas de Seguran�a:**

- ? Usar `.env.example` como template
- ? Nunca commitar arquivos `.env`
- ? Usar User Secrets para desenvolvimento
- ? Usar Azure Key Vault para produ��o
- ? Rotacionar chaves regularmente
- ? Nunca hardcodar credenciais no c�digo
- ? Nunca commitar connection strings
- ? Nunca compartilhar chaves em chat/email

## ?? **Verifica��o de Seguran�a:**

Antes de fazer push, verifique:

```bash
# Verificar se n�o h� credenciais nos arquivos
grep -r "SharedAccessKey" . --exclude-dir=.git
grep -r "Endpoint=sb://" . --exclude-dir=.git

# Verificar .gitignore
cat .gitignore | grep -E "\\.env|\*\\.env|secrets"
```

## ?? **Se credenciais foram expostas:**

1. **Revogar chaves imediatamente** no Azure Portal
2. **Gerar novas chaves**
3. **Limpar hist�rico do Git** se necess�rio
4. **Atualizar todas as configura��es**

## ?? **Suporte:**

Em caso de d�vidas sobre configura��o de seguran�a, consulte:
- [Azure Service Bus Security](https://docs.microsoft.com/azure/service-bus-messaging/service-bus-authentication-and-authorization)
- [ASP.NET Core Configuration Security](https://docs.microsoft.com/aspnet/core/security/app-secrets)