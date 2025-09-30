# ?? Configuração Segura de Credenciais

## ?? **IMPORTANTE - Não commitar credenciais!**

Este projeto requer configuração de credenciais do Azure Service Bus. **NUNCA** commite credenciais reais no Git.

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

### **1. Definir variável de ambiente:**
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

## ?? **Setup para Produção:**

### **1. Azure Key Vault:**
- Armazenar credenciais no Azure Key Vault
- Configurar Managed Identity
- Usar Azure App Configuration

### **2. Variáveis de Ambiente:**
- Configurar no Azure App Service
- Usar Azure DevOps Variables
- GitHub Secrets para CI/CD

## ?? **Boas Práticas de Segurança:**

- ? Usar `.env.example` como template
- ? Nunca commitar arquivos `.env`
- ? Usar User Secrets para desenvolvimento
- ? Usar Azure Key Vault para produção
- ? Rotacionar chaves regularmente
- ? Nunca hardcodar credenciais no código
- ? Nunca commitar connection strings
- ? Nunca compartilhar chaves em chat/email

## ?? **Verificação de Segurança:**

Antes de fazer push, verifique:

```bash
# Verificar se não há credenciais nos arquivos
grep -r "SharedAccessKey" . --exclude-dir=.git
grep -r "Endpoint=sb://" . --exclude-dir=.git

# Verificar .gitignore
cat .gitignore | grep -E "\\.env|\*\\.env|secrets"
```

## ?? **Se credenciais foram expostas:**

1. **Revogar chaves imediatamente** no Azure Portal
2. **Gerar novas chaves**
3. **Limpar histórico do Git** se necessário
4. **Atualizar todas as configurações**

## ?? **Suporte:**

Em caso de dúvidas sobre configuração de segurança, consulte:
- [Azure Service Bus Security](https://docs.microsoft.com/azure/service-bus-messaging/service-bus-authentication-and-authorization)
- [ASP.NET Core Configuration Security](https://docs.microsoft.com/aspnet/core/security/app-secrets)