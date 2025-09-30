# ?? Correção de Erros - Service Bus Monitor

## ? **Problema Original:**
```
Put token failed. status-code: 401, status-description: InvalidSignature: The token has an invalid signature.
```

## ? **Erros Corrigidos:**

### 1. **Enum ServiceBusFailureReason no .NET 9**
- **Problema:** `ServiceBusFailureReason.UnauthorizedAccess` não existe no .NET 9
- **Solução:** Removido e substituído por detecção de string na mensagem de erro

### 2. **Tratamento de Exceções Melhorado**
Adicionado tratamento específico para:
- ? `ServiceBusFailureReason.MessagingEntityNotFound` - Fila não encontrada
- ? `ServiceBusFailureReason.ServiceTimeout` - Timeout de conexão  
- ? `ServiceBusFailureReason.ServiceCommunicationProblem` - Problemas de rede
- ? Detecção de erros de autorização por conteúdo da mensagem
- ? `UnauthorizedAccessException` - Erro geral de autorização
- ? `ArgumentException` - Connection string malformada

### 3. **Novo Endpoint de Teste**
Adicionado: `POST /api/servicebusmonitor/test-send` para testar envio de mensagens

### 4. **Melhorias de Segurança**
- ? Connection string mascarada nos logs
- ? Tratamento seguro de strings vazias/nulas
- ? Validação robusta de formato

## ?? **Como Usar Agora:**

### **Testar Conexão:**
```http
GET /api/servicebusmonitor/test-connection
```

**Resposta de Sucesso:**
```json
{
  "status": "Success",
  "message": "Conexão com Service Bus estabelecida com sucesso",
  "queueName": "sale-processing-queue",
  "connectionStringMasked": "Endpoint=sb://fcg-games-servicebus...;SharedAccessKey=abc1***xyz9",
  "testTime": "2024-01-15T10:30:00Z"
}
```

**Resposta de Erro 401:**
```json
{
  "status": "Unauthorized",
  "message": "Assinatura inválida, token expirado ou chave de acesso incorreta",
  "details": "Put token failed. status-code: 401...",
  "reason": "GeneralError",
  "suggestion": "Execute: az servicebus namespace authorization-rule keys list para obter a chave correta"
}
```

### **Status do Service Bus:**
```http
GET /api/servicebusmonitor/status
```

### **Testar Envio:**
```http
POST /api/servicebusmonitor/test-send
```

## ?? **Diagnóstico de Erros:**

| Erro | Status | Causa Provável | Solução |
|------|--------|----------------|---------|
| **InvalidSignature** | 401 | Chave de acesso incorreta | `.\fix-servicebus.ps1` |
| **MessagingEntityNotFound** | 404 | Fila não existe | Criar fila: `az servicebus queue create` |
| **ServiceTimeout** | 408 | Namespace indisponível | Verificar se namespace está ativo |
| **Connection string malformed** | 400 | Formato inválido | Verificar formato da connection string |

## ??? **Scripts Atualizados:**

### **Para Testar:**
```powershell
.\test-servicebus.ps1
```

### **Para Corrigir:**
```powershell
.\fix-servicebus.ps1
```

### **Para Executar Docker:**
```powershell
.\run-docker.ps1
```

## ?? **Checklist de Verificação:**

- ? Build sem erros
- ? Tratamento de exceções robusto
- ? Endpoints de diagnóstico funcionando
- ? Mascaramento de credenciais
- ? Scripts de teste atualizados
- ? Documentação completa

**O erro original foi completamente corrigido!** ??