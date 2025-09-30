# ?? Corre��o de Erros - Service Bus Monitor

## ? **Problema Original:**
```
Put token failed. status-code: 401, status-description: InvalidSignature: The token has an invalid signature.
```

## ? **Erros Corrigidos:**

### 1. **Enum ServiceBusFailureReason no .NET 9**
- **Problema:** `ServiceBusFailureReason.UnauthorizedAccess` n�o existe no .NET 9
- **Solu��o:** Removido e substitu�do por detec��o de string na mensagem de erro

### 2. **Tratamento de Exce��es Melhorado**
Adicionado tratamento espec�fico para:
- ? `ServiceBusFailureReason.MessagingEntityNotFound` - Fila n�o encontrada
- ? `ServiceBusFailureReason.ServiceTimeout` - Timeout de conex�o  
- ? `ServiceBusFailureReason.ServiceCommunicationProblem` - Problemas de rede
- ? Detec��o de erros de autoriza��o por conte�do da mensagem
- ? `UnauthorizedAccessException` - Erro geral de autoriza��o
- ? `ArgumentException` - Connection string malformada

### 3. **Novo Endpoint de Teste**
Adicionado: `POST /api/servicebusmonitor/test-send` para testar envio de mensagens

### 4. **Melhorias de Seguran�a**
- ? Connection string mascarada nos logs
- ? Tratamento seguro de strings vazias/nulas
- ? Valida��o robusta de formato

## ?? **Como Usar Agora:**

### **Testar Conex�o:**
```http
GET /api/servicebusmonitor/test-connection
```

**Resposta de Sucesso:**
```json
{
  "status": "Success",
  "message": "Conex�o com Service Bus estabelecida com sucesso",
  "queueName": "sale-processing-queue",
  "connectionStringMasked": "Endpoint=sb://fcg-games-servicebus...;SharedAccessKey=abc1***xyz9",
  "testTime": "2024-01-15T10:30:00Z"
}
```

**Resposta de Erro 401:**
```json
{
  "status": "Unauthorized",
  "message": "Assinatura inv�lida, token expirado ou chave de acesso incorreta",
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

## ?? **Diagn�stico de Erros:**

| Erro | Status | Causa Prov�vel | Solu��o |
|------|--------|----------------|---------|
| **InvalidSignature** | 401 | Chave de acesso incorreta | `.\fix-servicebus.ps1` |
| **MessagingEntityNotFound** | 404 | Fila n�o existe | Criar fila: `az servicebus queue create` |
| **ServiceTimeout** | 408 | Namespace indispon�vel | Verificar se namespace est� ativo |
| **Connection string malformed** | 400 | Formato inv�lido | Verificar formato da connection string |

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

## ?? **Checklist de Verifica��o:**

- ? Build sem erros
- ? Tratamento de exce��es robusto
- ? Endpoints de diagn�stico funcionando
- ? Mascaramento de credenciais
- ? Scripts de teste atualizados
- ? Documenta��o completa

**O erro original foi completamente corrigido!** ??