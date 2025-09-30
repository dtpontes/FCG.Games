using FCG.Games.Presentation.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Azure.Messaging.ServiceBus;

namespace FCG.Games.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceBusMonitorController : ControllerBase
    {
        private readonly ServiceBusSettings _settings;
        private readonly ILogger<ServiceBusMonitorController> _logger;

        public ServiceBusMonitorController(
            IOptions<ServiceBusSettings> settings,
            ILogger<ServiceBusMonitorController> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Retorna informações sobre a configuração atual do Service Bus.
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetServiceBusStatus()
        {
            try
            {
                var status = new
                {
                    IsConfigured = !string.IsNullOrEmpty(_settings.ConnectionString),
                    QueueName = _settings.SalesQueueName,
                    MaxConcurrentCalls = _settings.MaxConcurrentCalls,
                    MessageTimeoutSeconds = _settings.MessageTimeoutSeconds,
                    ConnectionStringConfigured = !_settings.ConnectionString.Contains("your-access-key"),
                    LastCheck = DateTime.UtcNow,
                    ConnectionStringMasked = MaskConnectionString(_settings.ConnectionString)
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar status do Service Bus");
                return StatusCode(500, new { Message = "Erro ao verificar status", Error = ex.Message });
            }
        }

        /// <summary>
        /// Testa a conexão com o Service Bus.
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                _logger.LogInformation("Testando conexão com Service Bus");

                if (string.IsNullOrEmpty(_settings.ConnectionString))
                {
                    return BadRequest(new { Message = "Connection string não configurada" });
                }

                // Testar conexão básica
                await using var client = new ServiceBusClient(_settings.ConnectionString);
                
                // Verificar se consegue criar um receiver (isso testa a conexão e a fila)
                await using var receiver = client.CreateReceiver(_settings.SalesQueueName);
                
                var testResult = new
                {
                    Status = "Success",
                    Message = "Conexão com Service Bus estabelecida com sucesso",
                    QueueName = _settings.SalesQueueName,
                    ConnectionStringMasked = MaskConnectionString(_settings.ConnectionString),
                    TestTime = DateTime.UtcNow
                };

                _logger.LogInformation("Teste de conexão Service Bus: Sucesso");
                return Ok(testResult);
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                _logger.LogError(ex, "Fila não encontrada: {QueueName}", _settings.SalesQueueName);
                return NotFound(new 
                { 
                    Status = "QueueNotFound",
                    Message = $"Fila '{_settings.SalesQueueName}' não encontrada",
                    Details = ex.Message,
                    Reason = ex.Reason.ToString(),
                    Suggestion = $"Execute: az servicebus queue create --resource-group fcg-games-rg --namespace-name fcg-games-servicebus --name {_settings.SalesQueueName}"
                });
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.ServiceTimeout)
            {
                _logger.LogError(ex, "Timeout ao conectar com Service Bus");
                return StatusCode(408, new 
                { 
                    Status = "Timeout",
                    Message = "Timeout ao conectar com Service Bus",
                    Details = ex.Message,
                    Reason = ex.Reason.ToString(),
                    Suggestion = "Verifique se o namespace está ativo e acessível"
                });
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.ServiceCommunicationProblem)
            {
                _logger.LogError(ex, "Problema de comunicação com Service Bus");
                return StatusCode(503, new 
                { 
                    Status = "CommunicationError",
                    Message = "Problema de comunicação com Service Bus",
                    Details = ex.Message,
                    Reason = ex.Reason.ToString(),
                    Suggestion = "Verifique conectividade de rede e se o serviço está disponível"
                });
            }
            catch (ServiceBusException ex) when (ex.Message.Contains("authorization") || 
                                                 ex.Message.Contains("Unauthorized") || 
                                                 ex.Message.Contains("InvalidSignature") ||
                                                 ex.Message.Contains("401") ||
                                                 ex.Message.Contains("invalid signature"))
            {
                _logger.LogError(ex, "Erro de autorização detectado na mensagem: {Message}", ex.Message);
                return StatusCode(401, new 
                { 
                    Status = "Unauthorized",
                    Message = "Assinatura inválida, token expirado ou chave de acesso incorreta",
                    Details = ex.Message,
                    Reason = ex.Reason.ToString(),
                    Suggestion = "Verifique se a connection string está correta e obtenha uma nova chave: az servicebus namespace authorization-rule keys list"
                });
            }
            catch (ServiceBusException ex)
            {
                _logger.LogError(ex, "Erro do Service Bus: {Reason}", ex.Reason);
                
                // Verificar se é erro de autorização baseado no status code da exceção
                var statusCode = ex.Message.Contains("401") || ex.Message.ToLower().Contains("unauthorized") ? 401 : 500;
                
                return StatusCode(statusCode, new 
                { 
                    Status = statusCode == 401 ? "Unauthorized" : "ServiceBusError",
                    Message = statusCode == 401 ? "Erro de autorização" : ex.Message,
                    Reason = ex.Reason.ToString(),
                    Details = ex.Message,
                    Suggestion = statusCode == 401 
                        ? "Execute: az servicebus namespace authorization-rule keys list para obter a chave correta"
                        : "Verifique a configuração e logs detalhados"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Erro de autorização geral ao conectar com Service Bus");
                return StatusCode(401, new 
                { 
                    Status = "Unauthorized",
                    Message = "Token inválido ou acesso negado",
                    Details = ex.Message,
                    Suggestion = "Verifique se a connection string está correta e se o namespace existe"
                });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("connection string"))
            {
                _logger.LogError(ex, "Connection string inválida");
                return BadRequest(new 
                { 
                    Status = "InvalidConnectionString",
                    Message = "Connection string malformada ou inválida",
                    Details = ex.Message,
                    Suggestion = "Verifique o formato da connection string no arquivo de configuração"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao testar conexão Service Bus");
                return StatusCode(500, new 
                { 
                    Status = "Error",
                    Message = "Erro inesperado ao testar conexão",
                    Details = ex.Message,
                    ExceptionType = ex.GetType().Name,
                    Suggestion = "Verifique os logs para mais detalhes"
                });
            }
        }

        /// <summary>
        /// Verifica mensagens na fila de dead letter.
        /// </summary>
        [HttpGet("dead-letter-messages")]
        public async Task<IActionResult> GetDeadLetterMessages([FromQuery] int maxMessages = 10)
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.ConnectionString))
                {
                    return BadRequest(new { Message = "Connection string não configurada" });
                }

                await using var client = new ServiceBusClient(_settings.ConnectionString);
                await using var receiver = client.CreateReceiver(_settings.SalesQueueName, new ServiceBusReceiverOptions
                {
                    SubQueue = SubQueue.DeadLetter
                });

                var messages = new List<object>();
                var receivedMessages = await receiver.ReceiveMessagesAsync(maxMessages, TimeSpan.FromSeconds(10));

                foreach (var message in receivedMessages)
                {
                    var messageInfo = new
                    {
                        MessageId = message.MessageId,
                        CorrelationId = message.CorrelationId,
                        ContentType = message.ContentType,
                        EnqueuedTime = message.EnqueuedTime,
                        DeadLetterErrorDescription = message.DeadLetterErrorDescription,
                        DeadLetterReason = message.DeadLetterReason,
                        Body = message.Body.ToString(),
                        ApplicationProperties = message.ApplicationProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    };
                    messages.Add(messageInfo);

                    // Log para debug
                    _logger.LogInformation("Dead Letter Message: {MessageId}, Reason: {Reason}, Description: {Description}, Body: {Body}", 
                        message.MessageId, message.DeadLetterReason, message.DeadLetterErrorDescription, message.Body.ToString());
                }

                return Ok(new
                {
                    QueueName = _settings.SalesQueueName,
                    DeadLetterMessagesCount = messages.Count,
                    MaxRequested = maxMessages,
                    Messages = messages,
                    CheckTime = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar mensagens dead letter");
                return StatusCode(500, new { Message = "Erro ao verificar dead letter", Error = ex.Message });
            }
        }

        /// <summary>
        /// Retorna informações de performance e logs recentes.
        /// </summary>
        [HttpGet("health")]
        public IActionResult GetServiceBusHealth()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var health = new
                {
                    ProcessId = process.Id,
                    StartTime = process.StartTime,
                    WorkingSet = process.WorkingSet64 / 1024 / 1024, // MB
                    ServiceBusConfig = new
                    {
                        QueueName = _settings.SalesQueueName,
                        MaxConcurrentCalls = _settings.MaxConcurrentCalls,
                        MessageTimeout = _settings.MessageTimeoutSeconds,
                        IsConnectionStringConfigured = !string.IsNullOrEmpty(_settings.ConnectionString) && 
                                                     !_settings.ConnectionString.Contains("your-access-key")
                    },
                    Status = "Running",
                    Timestamp = DateTime.UtcNow
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar saúde do Service Bus");
                return StatusCode(500, new { Message = "Erro ao verificar saúde", Error = ex.Message });
            }
        }

        /// <summary>
        /// Testa o envio de uma mensagem simples para verificar se a fila está funcionando.
        /// </summary>
        [HttpPost("test-send")]
        public async Task<IActionResult> TestSendMessage()
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.ConnectionString))
                {
                    return BadRequest(new { Message = "Connection string não configurada" });
                }

                await using var client = new ServiceBusClient(_settings.ConnectionString);
                await using var sender = client.CreateSender(_settings.SalesQueueName);

                var testMessage = new ServiceBusMessage($"Test message from ServiceBusMonitor at {DateTime.UtcNow}")
                {
                    MessageId = Guid.NewGuid().ToString(),
                    ContentType = "text/plain"
                };

                await sender.SendMessageAsync(testMessage);

                _logger.LogInformation("Mensagem de teste enviada com sucesso. MessageId: {MessageId}", testMessage.MessageId);

                return Ok(new
                {
                    Status = "Success",
                    Message = "Mensagem de teste enviada com sucesso",
                    MessageId = testMessage.MessageId,
                    QueueName = _settings.SalesQueueName,
                    SentAt = DateTime.UtcNow
                });
            }
            catch (ServiceBusException ex)
            {
                _logger.LogError(ex, "Erro do Service Bus ao enviar mensagem de teste: {Reason}", ex.Reason);
                return StatusCode(500, new
                {
                    Status = "ServiceBusError",
                    Message = "Erro ao enviar mensagem de teste",
                    Details = ex.Message,
                    Reason = ex.Reason.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao enviar mensagem de teste");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "Erro inesperado ao enviar mensagem de teste",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Mascara a connection string para não expor a chave completa nos logs.
        /// </summary>
        private static string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "Not configured";

            try
            {
                // Mascarar a SharedAccessKey
                var parts = connectionString.Split(';');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("SharedAccessKey=", StringComparison.OrdinalIgnoreCase))
                    {
                        var key = parts[i].Substring("SharedAccessKey=".Length);
                        if (key.Length > 8)
                        {
                            parts[i] = $"SharedAccessKey={key.Substring(0, 4)}***{key.Substring(key.Length - 4)}";
                        }
                        else if (key.Length > 0)
                        {
                            parts[i] = "SharedAccessKey=***";
                        }
                    }
                }

                return string.Join(';', parts);
            }
            catch
            {
                return "Invalid connection string format";
            }
        }
    }
}