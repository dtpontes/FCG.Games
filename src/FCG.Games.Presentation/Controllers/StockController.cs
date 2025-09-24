using FCG.Games.Domain.Core.Notifications;
using FCG.Games.Domain.Interfaces.Commons;
using FCG.Games.Service.DTO.Request;
using FCG.Games.Service.DTO.Response;
using FCG.Games.Service.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Games.Presentation.Controllers
{
    /// <summary>
    /// Controlador para opera��es relacionadas ao estoque de jogos.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : BaseController
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StockController> _logger;

        public StockController(IStockService stockService,
                              IMediatorHandler mediator,
                              INotificationHandler<DomainNotification> notifications,
                              ILogger<StockController> logger) : base(notifications, mediator)
        {
            _stockService = stockService;
            _logger = logger;
        }

        /// <summary>
        /// Adiciona quantidade ao estoque de um jogo espec�fico.
        /// Se n�o existir registro de estoque, cria um novo.
        /// </summary>
        /// <param name="addStockRequestDto">Dados para adi��o ao estoque</param>
        /// <returns>Informa��es do estoque atualizado</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("add")]
        [ProducesResponseType(typeof(StockResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> AddStock([FromBody] AddStockRequestDto addStockRequestDto)
        {
            try
            {
                _logger.LogInformation("Adicionando {Quantity} unidades ao estoque do jogo {GameId}", 
                    addStockRequestDto.Quantity, addStockRequestDto.GameId);

                var result = await _stockService.AddStockAsync(addStockRequestDto);

                if (result != null)
                {
                    _logger.LogInformation("Estoque atualizado com sucesso. Jogo: {GameId}, Nova quantidade: {Quantity}", 
                        result.GameId, result.Quantity);
                    return Response(result);
                }

                return Response();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar estoque para o jogo {GameId}", addStockRequestDto.GameId);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Subtrai quantidade do estoque de um jogo espec�fico ap�s uma venda.
        /// Valida se h� estoque suficiente antes de realizar a opera��o.
        /// </summary>
        /// <param name="subStockRequestDto">Dados para subtra��o do estoque</param>
        /// <returns>Informa��es do estoque atualizado</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("sub")]
        [ProducesResponseType(typeof(StockResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> SubStock([FromBody] SubStockRequestDto subStockRequestDto)
        {
            try
            {
                _logger.LogInformation("Subtraindo {Quantity} unidades do estoque do jogo {GameId}", 
                    subStockRequestDto.Quantity, subStockRequestDto.GameId);

                var result = await _stockService.SubStockAsync(subStockRequestDto);

                if (result != null)
                {
                    _logger.LogInformation("Estoque reduzido com sucesso. Jogo: {GameId}, Nova quantidade: {Quantity}", 
                        result.GameId, result.Quantity);
                    return Response(result);
                }

                return Response();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao subtrair estoque para o jogo {GameId}", subStockRequestDto.GameId);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obt�m informa��es do estoque de um jogo espec�fico pelo ID.
        /// </summary>
        /// <param name="gameId">ID do jogo</param>
        /// <returns>Informa��es do estoque do jogo</returns>
        [HttpGet("game/{gameId}")]
        [ProducesResponseType(typeof(StockResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStockByGameId(long gameId)
        {
            try
            {
                _logger.LogInformation("Consultando estoque do jogo {GameId}", gameId);

                var stock = await _stockService.GetStockByGameIdAsync(gameId);

                if (stock != null)
                {
                    return Response(stock);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar estoque do jogo {GameId}", gameId);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obt�m todos os registros de estoque.
        /// </summary>
        /// <returns>Lista com todos os estoques</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<StockResponseDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetAllStocks()
        {
            try
            {
                _logger.LogInformation("Consultando todos os registros de estoque");

                var stocks = await _stockService.GetAllStocksAsync();
                return Response(stocks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar todos os estoques");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Endpoint para verificar se h� estoque dispon�vel para um jogo.
        /// </summary>
        /// <param name="gameId">ID do jogo</param>
        /// <param name="requiredQuantity">Quantidade necess�ria (padr�o: 1)</param>
        /// <returns>Informa��o se h� estoque suficiente</returns>
        [HttpGet("availability/{gameId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CheckStockAvailability(long gameId, [FromQuery] int requiredQuantity = 1)
        {
            try
            {
                _logger.LogInformation("Verificando disponibilidade de estoque. Jogo: {GameId}, Quantidade: {RequiredQuantity}", 
                    gameId, requiredQuantity);

                var stock = await _stockService.GetStockByGameIdAsync(gameId);

                if (stock == null)
                {
                    return NotFound();
                }

                var isAvailable = stock.Quantity >= requiredQuantity;

                return Ok(new
                {
                    GameId = gameId,
                    GameName = stock.GameName,
                    AvailableQuantity = stock.Quantity,
                    RequiredQuantity = requiredQuantity,
                    IsAvailable = isAvailable,
                    Message = isAvailable ? "Estoque dispon�vel" : "Estoque insuficiente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar disponibilidade de estoque do jogo {GameId}", gameId);
                return StatusCode(500, "Erro interno do servidor");
            }
        }
    }
}