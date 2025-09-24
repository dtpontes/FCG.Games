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
    /// Serviço responsável pelo processamento de mensagens de venda e débito automático de estoque.
    /// </summary>
    public class SaleProcessingService : BaseService, ISaleProcessingService
    {
        private readonly IStockService _stockService;
        private readonly ILogger<SaleProcessingService> _logger;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="SaleProcessingService"/>.
        /// </summary>
        /// <param name="stockService">Serviço de estoque.</param>
        /// <param name="notifications">Handler de notificações de domínio.</param>
        /// <param name="mediator">Handler de eventos do domínio.</param>
        /// <param name="logger">Logger para auditoria e depuração.</param>
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
                    response.Message = "Dados da venda inválidos";
                    response.Errors = validationErrors;
                    return response;
                }

                // Verificar se há estoque disponível antes de debitar
                var currentStock = await _stockService.GetStockByGameIdAsync(saleMessage.GameId);
                if (currentStock == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Jogo não encontrado ou sem registro de estoque";
                    response.Errors.Add($"Não existe registro de estoque para o jogo ID {saleMessage.GameId}");
                    
                    _logger.LogWarning("Tentativa de débito em jogo sem estoque. TransactionId: {TransactionId}, GameId: {GameId}",
                        saleMessage.TransactionId, saleMessage.GameId);
                    
                    return response;
                }

                response.GameName = currentStock.GameName;

                if (currentStock.Quantity < saleMessage.Quantity)
                {
                    response.IsSuccess = false;
                    response.Message = "Estoque insuficiente para completar a venda";
                    response.RemainingStock = currentStock.Quantity;
                    response.Errors.Add($"Estoque disponível: {currentStock.Quantity}, Quantidade solicitada: {saleMessage.Quantity}");
                    
                    _logger.LogWarning("Estoque insuficiente para venda. TransactionId: {TransactionId}, GameId: {GameId}, Disponível: {Available}, Solicitado: {Requested}",
                        saleMessage.TransactionId, saleMessage.GameId, currentStock.Quantity, saleMessage.Quantity);
                    
                    return response;
                }

                // Criar a requisição de subtração de estoque
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
                    response.Message = "Erro ao processar débito no estoque";
                    response.Errors.Add("Falha na operação de subtração do estoque");
                    
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
        /// <returns>Lista de erros de validação.</returns>
        private static List<string> ValidateSaleMessage(SaleMessageDto saleMessage)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(saleMessage.TransactionId))
                errors.Add("ID da transação é obrigatório");

            if (saleMessage.GameId <= 0)
                errors.Add("ID do jogo deve ser maior que zero");

            if (saleMessage.Quantity <= 0)
                errors.Add("Quantidade deve ser maior que zero");

            if (saleMessage.Quantity > 10000)
                errors.Add("Quantidade não pode ser maior que 10.000 unidades");

            if (saleMessage.SaleDateTime == default)
                errors.Add("Data da venda é obrigatória");

            if (string.IsNullOrWhiteSpace(saleMessage.UserId))
                errors.Add("ID do usuário é obrigatório");

            if (saleMessage.TotalAmount <= 0)
                errors.Add("Valor total da venda deve ser maior que zero");

            return errors;
        }
    }
}