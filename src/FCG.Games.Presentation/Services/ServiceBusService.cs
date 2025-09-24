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
                var saleMessage = JsonSerializer.Deserialize<SaleMessageDto>(messageBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (saleMessage == null)
                {
                    _logger.LogError("Falha ao deserializar mensagem. MessageId: {MessageId}", messageId);
                    await args.DeadLetterMessageAsync(args.Message, "DESERIALIZATION_ERROR", "Não foi possível deserializar a mensagem");
                    return;
                }

                // Adicionar IDs da mensagem para auditoria
                if (string.IsNullOrEmpty(saleMessage.TransactionId))
                {
                    saleMessage.TransactionId = messageId ?? Guid.NewGuid().ToString();
                }

                // Processar a venda
                var result = await saleProcessingService.ProcessSaleAsync(saleMessage);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Venda processada com sucesso. TransactionId: {TransactionId}, MessageId: {MessageId}, GameId: {GameId}", 
                        result.TransactionId, messageId, result.GameId);
                    
                    // Completar a mensagem (remove da fila)
                    await args.CompleteMessageAsync(args.Message);
                }
                else
                {
                    _logger.LogWarning("Falha ao processar venda. TransactionId: {TransactionId}, MessageId: {MessageId}, Errors: {Errors}", 
                        result.TransactionId, messageId, string.Join("; ", result.Errors));

                    // Se for erro de estoque insuficiente ou jogo não encontrado, enviar para dead letter
                    if (result.Errors.Any(e => e.Contains("estoque") || e.Contains("jogo não encontrado")))
                    {
                        await args.DeadLetterMessageAsync(args.Message, "BUSINESS_RULE_ERROR", result.Message);
                    }
                    else
                    {
                        // Para outros erros, abandonar mensagem para reprocessamento
                        await args.AbandonMessageAsync(args.Message);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro de JSON ao processar mensagem. MessageId: {MessageId}", messageId);
                await args.DeadLetterMessageAsync(args.Message, "JSON_ERROR", $"Erro ao deserializar JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar mensagem. MessageId: {MessageId}", messageId);
                
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