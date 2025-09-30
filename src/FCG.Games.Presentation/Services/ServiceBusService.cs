using Azure.Messaging.ServiceBus;
using FCG.Games.Service.DTO.Messages;
using FCG.Games.Service.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FCG.Games.Presentation.Services
{
    /// <summary>
    /// Configurações do Azure Service Bus.
    /// </summary>
    public class ServiceBusSettings
    {
        /// <summary>
        /// String de conexão do Service Bus.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Nome da fila para receber mensagens de venda.
        /// </summary>
        public string SalesQueueName { get; set; } = "sales-queue";

        /// <summary>
        /// Quantidade máxima de mensagens processadas simultaneamente.
        /// </summary>
        public int MaxConcurrentCalls { get; set; } = 5;

        /// <summary>
        /// Tempo limite para processamento de mensagem em segundos.
        /// </summary>
        public int MessageTimeoutSeconds { get; set; } = 300;
    }

    /// <summary>
    /// Serviço para integração com Azure Service Bus.
    /// </summary>
    public class ServiceBusService : IAsyncDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusProcessor _processor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServiceBusService> _logger;
        private readonly ServiceBusSettings _settings;

        public ServiceBusService(
            IOptions<ServiceBusSettings> settings,
            IServiceProvider serviceProvider,
            ILogger<ServiceBusService> logger)
        {
            _settings = settings.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;

            _client = new ServiceBusClient(_settings.ConnectionString);
            _processor = _client.CreateProcessor(_settings.SalesQueueName, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = _settings.MaxConcurrentCalls,
                AutoCompleteMessages = false,
                MaxAutoLockRenewalDuration = TimeSpan.FromSeconds(_settings.MessageTimeoutSeconds)
            });

            _processor.ProcessMessageAsync += MessageHandler;
            _processor.ProcessErrorAsync += ErrorHandler;
        }

        /// <summary>
        /// Inicia o processamento de mensagens.
        /// </summary>
        public async Task StartProcessingAsync()
        {
            _logger.LogInformation("Iniciando processamento de mensagens do Service Bus. Queue: {QueueName}", _settings.SalesQueueName);
            await _processor.StartProcessingAsync();
        }

        /// <summary>
        /// Para o processamento de mensagens.
        /// </summary>
        public async Task StopProcessingAsync()
        {
            _logger.LogInformation("Parando processamento de mensagens do Service Bus");
            await _processor.StopProcessingAsync();
        }

        /// <summary>
        /// Manipula as mensagens recebidas da fila.
        /// </summary>
        /// <param name="args">Argumentos da mensagem.</param>
        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            var messageId = args.Message.MessageId;
            var correlationId = args.Message.CorrelationId;

            // Create a scope for each message to resolve scoped dependencies
            using var scope = _serviceProvider.CreateScope();
            var saleProcessingService = scope.ServiceProvider.GetRequiredService<ISaleProcessingService>();

            try
            {
                _logger.LogInformation("Processando mensagem. MessageId: {MessageId}, CorrelationId: {CorrelationId}", 
                    messageId, correlationId);

                // Deserializar a mensagem
                var messageBody = args.Message.Body.ToString();
                _logger.LogInformation("Conteúdo da mensagem recebida: {MessageBody}", messageBody);

                // Configurar JsonSerializer com opções mais permissivas
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                SaleMessageDto? saleMessage;
                try
                {
                    saleMessage = JsonSerializer.Deserialize<SaleMessageDto>(messageBody, jsonOptions);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Erro específico de JSON ao deserializar mensagem. MessageId: {MessageId}, MessageBody: {MessageBody}", 
                        messageId, messageBody);
                    await args.DeadLetterMessageAsync(args.Message, "JSON_DESERIALIZATION_ERROR", $"Erro ao deserializar JSON: {jsonEx.Message}");
                    return;
                }

                if (saleMessage == null)
                {
                    _logger.LogError("Mensagem deserializada como null. MessageId: {MessageId}, MessageBody: {MessageBody}", 
                        messageId, messageBody);
                    await args.DeadLetterMessageAsync(args.Message, "NULL_DESERIALIZATION_ERROR", "Mensagem deserializada como null");
                    return;
                }

                // Log dos dados deserializados para debug
                _logger.LogInformation("Mensagem deserializada com sucesso. TransactionId: {TransactionId}, GameId: {GameId}, Quantity: {Quantity}, UserId: {UserId}", 
                    saleMessage.TransactionId, saleMessage.GameId, saleMessage.Quantity, saleMessage.UserId);

                // Adicionar IDs da mensagem para auditoria
                if (string.IsNullOrEmpty(saleMessage.TransactionId))
                {
                    saleMessage.TransactionId = messageId ?? Guid.NewGuid().ToString();
                    _logger.LogInformation("TransactionId vazio, atribuído novo ID: {TransactionId}", saleMessage.TransactionId);
                }

                // Processar a venda
                _logger.LogInformation("Iniciando processamento da venda. TransactionId: {TransactionId}", saleMessage.TransactionId);
                var result = await saleProcessingService.ProcessSaleAsync(saleMessage);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Venda processada com sucesso. TransactionId: {TransactionId}, MessageId: {MessageId}, GameId: {GameId}, RemainingStock: {RemainingStock}", 
                        result.TransactionId, messageId, result.GameId, result.RemainingStock);
                    
                    // Completar a mensagem (remove da fila)
                    await args.CompleteMessageAsync(args.Message);
                }
                else
                {
                    _logger.LogWarning("Falha ao processar venda. TransactionId: {TransactionId}, MessageId: {MessageId}, Errors: {Errors}, Message: {Message}", 
                        result.TransactionId, messageId, string.Join("; ", result.Errors), result.Message);

                    // Se for erro de estoque insuficiente ou jogo não encontrado, enviar para dead letter
                    if (result.Errors.Any(e => e.Contains("estoque") || e.Contains("jogo não encontrado") || e.Contains("Jogo com ID") || e.Contains("estoque para o jogo")))
                    {
                        _logger.LogWarning("Enviando mensagem para Dead Letter devido a regra de negócio. TransactionId: {TransactionId}", result.TransactionId);
                        await args.DeadLetterMessageAsync(args.Message, "BUSINESS_RULE_ERROR", result.Message);
                    }
                    else
                    {
                        // Para outros erros, abandonar mensagem para reprocessamento
                        _logger.LogInformation("Abandonando mensagem para reprocessamento. TransactionId: {TransactionId}", result.TransactionId);
                        await args.AbandonMessageAsync(args.Message);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro de JSON ao processar mensagem. MessageId: {MessageId}, MessageBody: {MessageBody}", 
                    messageId, args.Message.Body.ToString());
                await args.DeadLetterMessageAsync(args.Message, "JSON_ERROR", $"Erro ao deserializar JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar mensagem. MessageId: {MessageId}, ExceptionType: {ExceptionType}", 
                    messageId, ex.GetType().Name);
                
                // Para erros inesperados, abandona a mensagem para reprocessamento
                await args.AbandonMessageAsync(args.Message);
            }
        }

        /// <summary>
        /// Manipula erros do processador.
        /// </summary>
        /// <param name="args">Argumentos do erro.</param>
        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Erro no processador do Service Bus. Source: {Source}, EntityPath: {EntityPath}", 
                args.ErrorSource, args.EntityPath);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Envia uma mensagem de resposta para uma fila específica (opcional).
        /// </summary>
        /// <param name="queueName">Nome da fila de destino.</param>
        /// <param name="response">Resposta a ser enviada.</param>
        public async Task SendResponseAsync(string queueName, object response)
        {
            try
            {
                var sender = _client.CreateSender(queueName);
                var messageBody = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var message = new ServiceBusMessage(messageBody)
                {
                    ContentType = "application/json"
                };

                await sender.SendMessageAsync(message);
                _logger.LogInformation("Resposta enviada para fila {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar resposta para fila {QueueName}", queueName);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_processor != null)
            {
                await _processor.DisposeAsync();
            }

            if (_client != null)
            {
                await _client.DisposeAsync();
            }
        }
    }
}