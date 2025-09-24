using AutoMapper;
using FCG.Games.Domain.Core.Notifications;
using FCG.Games.Domain.Interfaces.Commons;
using FCG.Games.Service.DTO.Messages;
using FCG.Games.Service.DTO.Request;
using FCG.Games.Service.DTO.Response;
using FCG.Games.Service.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FCG.Games.Service
{
    /// <summary>
    /// Servi�o respons�vel pelo processamento de mensagens de venda e d�bito autom�tico de estoque.
    /// </summary>
    public class SaleProcessingService : BaseService, ISaleProcessingService
    {
        private readonly IStockService _stockService;
        private readonly ILogger<SaleProcessingService> _logger;

        /// <summary>
        /// Inicializa uma nova inst�ncia de <see cref="SaleProcessingService"/>.
        /// </summary>
        /// <param name="stockService">Servi�o de estoque.</param>
        /// <param name="notifications">Handler de notifica��es de dom�nio.</param>
        /// <param name="mediator">Handler de eventos do dom�nio.</param>
        /// <param name="logger">Logger para auditoria e depura��o.</param>
        public SaleProcessingService(
            IStockService stockService,
            INotificationHandler<DomainNotification> notifications,
            IMediatorHandler mediator,
            ILogger<SaleProcessingService> logger) : base(notifications, mediator)
        {
            _stockService = stockService;
            _logger = logger;
        }

        public async Task<SaleProcessingResponseDto> ProcessSaleAsync(SaleMessageDto saleMessage)
        {
            var response = new SaleProcessingResponseDto
            {
                TransactionId = saleMessage.TransactionId,
                GameId = saleMessage.GameId,
                ProcessedQuantity = saleMessage.Quantity,
                ProcessedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Iniciando processamento da venda. TransactionId: {TransactionId}, GameId: {GameId}, Quantity: {Quantity}",
                    saleMessage.TransactionId, saleMessage.GameId, saleMessage.Quantity);

                // Validar os dados da mensagem
                var validationErrors = ValidateSaleMessage(saleMessage);
                if (validationErrors.Count > 0)
                {
                    response.IsSuccess = false;
                    response.Message = "Dados da venda inv�lidos";
                    response.Errors = validationErrors;
                    return response;
                }

                // Verificar se h� estoque dispon�vel antes de debitar
                var currentStock = await _stockService.GetStockByGameIdAsync(saleMessage.GameId);
                if (currentStock == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Jogo n�o encontrado ou sem registro de estoque";
                    response.Errors.Add($"N�o existe registro de estoque para o jogo ID {saleMessage.GameId}");
                    
                    _logger.LogWarning("Tentativa de d�bito em jogo sem estoque. TransactionId: {TransactionId}, GameId: {GameId}",
                        saleMessage.TransactionId, saleMessage.GameId);
                    
                    return response;
                }

                response.GameName = currentStock.GameName;

                if (currentStock.Quantity < saleMessage.Quantity)
                {
                    response.IsSuccess = false;
                    response.Message = "Estoque insuficiente para completar a venda";
                    response.RemainingStock = currentStock.Quantity;
                    response.Errors.Add($"Estoque dispon�vel: {currentStock.Quantity}, Quantidade solicitada: {saleMessage.Quantity}");
                    
                    _logger.LogWarning("Estoque insuficiente para venda. TransactionId: {TransactionId}, GameId: {GameId}, Dispon�vel: {Available}, Solicitado: {Requested}",
                        saleMessage.TransactionId, saleMessage.GameId, currentStock.Quantity, saleMessage.Quantity);
                    
                    return response;
                }

                // Criar a requisi��o de subtra��o de estoque
                var subStockRequest = new SubStockRequestDto
                {
                    GameId = saleMessage.GameId,
                    Quantity = saleMessage.Quantity
                };

                // Debitar do estoque
                var stockResult = await _stockService.SubStockAsync(subStockRequest);
                
                if (stockResult != null)
                {
                    response.IsSuccess = true;
                    response.RemainingStock = stockResult.Quantity;
                    response.Message = $"Venda processada com sucesso. {saleMessage.Quantity} unidade(s) debitada(s) do estoque";
                    
                    _logger.LogInformation("Venda processada com sucesso. TransactionId: {TransactionId}, GameId: {GameId}, Quantidade debitada: {Quantity}, Estoque restante: {RemainingStock}",
                        saleMessage.TransactionId, saleMessage.GameId, saleMessage.Quantity, stockResult.Quantity);
                }
                else
                {
                    response.IsSuccess = false;
                    response.Message = "Erro ao processar d�bito no estoque";
                    response.Errors.Add("Falha na opera��o de subtra��o do estoque");
                    
                    _logger.LogError("Falha ao debitar estoque. TransactionId: {TransactionId}, GameId: {GameId}",
                        saleMessage.TransactionId, saleMessage.GameId);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar venda. TransactionId: {TransactionId}, GameId: {GameId}",
                    saleMessage.TransactionId, saleMessage.GameId);

                response.IsSuccess = false;
                response.Message = "Erro interno ao processar a venda";
                response.Errors.Add($"Erro: {ex.Message}");
                return response;
            }
        }

        /// <summary>
        /// Valida os dados da mensagem de venda.
        /// </summary>
        /// <param name="saleMessage">Mensagem de venda a ser validada.</param>
        /// <returns>Lista de erros de valida��o.</returns>
        private static List<string> ValidateSaleMessage(SaleMessageDto saleMessage)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(saleMessage.TransactionId))
                errors.Add("ID da transa��o � obrigat�rio");

            if (saleMessage.GameId <= 0)
                errors.Add("ID do jogo deve ser maior que zero");

            if (saleMessage.Quantity <= 0)
                errors.Add("Quantidade deve ser maior que zero");

            if (saleMessage.Quantity > 10000)
                errors.Add("Quantidade n�o pode ser maior que 10.000 unidades");

            if (saleMessage.SaleDateTime == default)
                errors.Add("Data da venda � obrigat�ria");

            if (string.IsNullOrWhiteSpace(saleMessage.UserId))
                errors.Add("ID do usu�rio � obrigat�rio");

            if (saleMessage.TotalAmount <= 0)
                errors.Add("Valor total da venda deve ser maior que zero");

            return errors;
        }
    }
}