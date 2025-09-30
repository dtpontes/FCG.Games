using Azure.Messaging.ServiceBus;
using FCG.Games.Service.DTO.Messages;
using FCG.Games.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using FCG.Games.Presentation.Services;

namespace FCG.Games.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ServiceBusSettings _serviceBusSettings;
        private readonly IStockService _stockService;
        private readonly ILogger<TestController> _logger;

        public TestController(
            IOptions<ServiceBusSettings> serviceBusSettings,
            IStockService stockService,
            ILogger<TestController> logger)
        {
            _serviceBusSettings = serviceBusSettings.Value;
            _stockService = stockService;
            _logger = logger;
        }

        /// <summary>
        /// Envia uma mensagem de teste para a fila de vendas.
        /// </summary>
        [HttpPost("send-sale-message")]
        public async Task<IActionResult> SendSaleMessage([FromBody] SaleMessageDto saleMessage)
        {
            try
            {
                if (string.IsNullOrEmpty(saleMessage.TransactionId))
                {
                    saleMessage.TransactionId = Guid.NewGuid().ToString();
                }

                if (saleMessage.SaleDateTime == default)
                {
                    saleMessage.SaleDateTime = DateTime.UtcNow;
                }

                if (string.IsNullOrEmpty(saleMessage.SourceService))
                {
                    saleMessage.SourceService = "FCG.Sales.Test";
                }

                await using var client = new ServiceBusClient(_serviceBusSettings.ConnectionString);
                await using var sender = client.CreateSender(_serviceBusSettings.SalesQueueName);

                var messageBody = JsonSerializer.Serialize(saleMessage, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var message = new ServiceBusMessage(messageBody)
                {
                    ContentType = "application/json",
                    MessageId = saleMessage.TransactionId,
                    CorrelationId = saleMessage.TransactionId
                };

                await sender.SendMessageAsync(message);

                _logger.LogInformation("Mensagem de teste enviada. TransactionId: {TransactionId}, GameId: {GameId}", 
                    saleMessage.TransactionId, saleMessage.GameId);

                return Ok(new { 
                    Message = "Mensagem enviada com sucesso", 
                    TransactionId = saleMessage.TransactionId,
                    QueueName = _serviceBusSettings.SalesQueueName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem de teste");
                return StatusCode(500, new { Message = "Erro ao enviar mensagem", Error = ex.Message });
            }
        }

        /// <summary>
        /// Cria uma mensagem de teste com dados pré-preenchidos.
        /// </summary>
        [HttpGet("create-sample-sale")]
        public async Task<IActionResult> CreateSampleSale([FromQuery] long gameId = 1, [FromQuery] int quantity = 1)
        {
            try
            {
                // Verificar se o jogo existe no estoque
                var stock = await _stockService.GetStockByGameIdAsync(gameId);
                if (stock == null)
                {
                    return BadRequest(new { Message = $"Jogo com ID {gameId} não encontrado no estoque" });
                }

                var saleMessage = new SaleMessageDto
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    GameId = gameId,
                    Quantity = quantity,
                    SaleDateTime = DateTime.UtcNow,
                    UserId = "test-user-123",
                    TotalAmount = 59.99m * quantity,
                    SourceService = "FCG.Sales.Test"
                };

                return Ok(new { 
                    Message = "Mensagem de exemplo criada. Use POST /api/test/send-sale-message para enviar",
                    SampleMessage = saleMessage,
                    CurrentStock = new {
                        GameId = stock.GameId,
                        GameName = stock.GameName,
                        Quantity = stock.Quantity
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar mensagem de exemplo");
                return StatusCode(500, new { Message = "Erro ao criar mensagem de exemplo", Error = ex.Message });
            }
        }

        /// <summary>
        /// Envia múltiplas mensagens de teste para simular carga.
        /// </summary>
        [HttpPost("send-multiple-sales")]
        public async Task<IActionResult> SendMultipleSales([FromQuery] long gameId = 1, [FromQuery] int count = 5)
        {
            try
            {
                var results = new List<object>();
                
                await using var client = new ServiceBusClient(_serviceBusSettings.ConnectionString);
                await using var sender = client.CreateSender(_serviceBusSettings.SalesQueueName);

                for (int i = 0; i < count; i++)
                {
                    var saleMessage = new SaleMessageDto
                    {
                        TransactionId = Guid.NewGuid().ToString(),
                        GameId = gameId,
                        Quantity = 1,
                        SaleDateTime = DateTime.UtcNow.AddSeconds(-i), // Para simular vendas em momentos diferentes
                        UserId = $"test-user-{i + 1}",
                        TotalAmount = 59.99m,
                        SourceService = "FCG.Sales.LoadTest"
                    };

                    var messageBody = JsonSerializer.Serialize(saleMessage, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    var message = new ServiceBusMessage(messageBody)
                    {
                        ContentType = "application/json",
                        MessageId = saleMessage.TransactionId,
                        CorrelationId = saleMessage.TransactionId
                    };

                    await sender.SendMessageAsync(message);

                    results.Add(new { 
                        Index = i + 1, 
                        TransactionId = saleMessage.TransactionId,
                        GameId = saleMessage.GameId,
                        UserId = saleMessage.UserId
                    });

                    // Pequeno delay entre envios para não sobrecarregar
                    await Task.Delay(100);
                }

                _logger.LogInformation("Enviadas {Count} mensagens de teste para GameId {GameId}", count, gameId);

                return Ok(new { 
                    Message = $"{count} mensagens enviadas com sucesso", 
                    QueueName = _serviceBusSettings.SalesQueueName,
                    Results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar múltiplas mensagens de teste");
                return StatusCode(500, new { Message = "Erro ao enviar mensagens", Error = ex.Message });
            }
        }
    }
}