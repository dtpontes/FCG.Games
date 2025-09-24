using FCG.Games.Service.DTO.Messages;
using FCG.Games.Service.DTO.Response;

namespace FCG.Games.Service.Interfaces
{
    /// <summary>
    /// Interface para o serviço de processamento de vendas.
    /// </summary>
    public interface ISaleProcessingService
    {
        /// <summary>
        /// Processa uma mensagem de venda, debitando automaticamente do estoque.
        /// </summary>
        /// <param name="saleMessage">Mensagem de venda recebida.</param>
        /// <returns>Resultado do processamento da venda.</returns>
        Task<SaleProcessingResponseDto> ProcessSaleAsync(SaleMessageDto saleMessage);
    }
}