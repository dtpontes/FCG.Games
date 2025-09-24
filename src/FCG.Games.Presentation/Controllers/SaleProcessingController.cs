using FCG.Games.Domain.Core.Notifications;
using FCG.Games.Domain.Interfaces.Commons;
using FCG.Games.Service.DTO.Messages;
using FCG.Games.Service.DTO.Response;
using FCG.Games.Service.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Games.Presentation.Controllers
{
    /// <summary>
    /// Controlador para operações relacionadas ao processamento de vendas.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SaleProcessingController : BaseController
    {
        private readonly ISaleProcessingService _saleProcessingService;
        private readonly ILogger<SaleProcessingController> _logger;

        public SaleProcessingController(
            ISaleProcessingService saleProcessingService,
            IMediatorHandler mediator,
            INotificationHandler<DomainNotification> notifications,
            ILogger<SaleProcessingController> logger) : base(notifications, mediator)
        {
            _saleProcessingService = saleProcessingService;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint para processar uma venda manualmente (para testes ou fallback).
        /// </summary>
        /// <param name="saleMessage">Dados da venda a ser processada</param>
        /// <returns>Resultado do processamento da venda</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("process-sale")]
        [ProducesResponseType(typeof(SaleProcessingResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> ProcessSale([FromBody] SaleMessageDto saleMessage)
        {
            try
            {
                _logger.LogInformation("Processamento manual de venda solicitado. TransactionId: {TransactionId}, GameId: {GameId}",
                    saleMessage.TransactionId, saleMessage.GameId);

                var result = await _saleProcessingService.ProcessSaleAsync(saleMessage);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Venda processada manualmente com sucesso. TransactionId: {TransactionId}",
                        result.TransactionId);
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("Falha no processamento manual da venda. TransactionId: {TransactionId}, Erros: {Errors}",
                        result.TransactionId, string.Join("; ", result.Errors));
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento manual da venda. TransactionId: {TransactionId}",
                    saleMessage.TransactionId);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Endpoint para verificar o status do processamento de vendas.
        /// </summary>
        /// <returns>Status dos serviços relacionados ao processamento de vendas</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("status")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public IActionResult GetProcessingStatus()
        {
            try
            {
                var status = new
                {
                    ServiceName = "Sale Processing Service",
                    Status = "Running",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                };

                _logger.LogInformation("Status do serviço de processamento consultado");
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar status do serviço");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Endpoint para simular uma venda (apenas para desenvolvimento/testes).
        /// </summary>
        /// <param name="gameId">ID do jogo</param>
        /// <param name="quantity">Quantidade a ser vendida</param>
        /// <returns>Mensagem de venda simulada</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("simulate-sale/{gameId}")]
        [ProducesResponseType(typeof(SaleMessageDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public IActionResult SimulateSale(long gameId, [FromQuery] int quantity = 1)
        {
            try
            {
                if (gameId <= 0 || quantity <= 0)
                {
                    return BadRequest("GameId e Quantity devem ser maiores que zero");
                }

                var simulatedSale = new SaleMessageDto
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    GameId = gameId,
                    Quantity = quantity,
                    SaleDateTime = DateTime.UtcNow,
                    UserId = "test-user-" + Random.Shared.Next(1000, 9999),
                    TotalAmount = Random.Shared.Next(10, 100) * quantity,
                    SourceService = "FCG.Games.Test"
                };

                _logger.LogInformation("Venda simulada criada. TransactionId: {TransactionId}, GameId: {GameId}, Quantity: {Quantity}",
                    simulatedSale.TransactionId, simulatedSale.GameId, simulatedSale.Quantity);

                return Ok(simulatedSale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao simular venda para o jogo {GameId}", gameId);
                return StatusCode(500, "Erro interno do servidor");
            }
        }
    }
}