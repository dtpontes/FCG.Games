using FCG.Games.Service.DTO.Request;
using FCG.Games.Service.DTO.Response;

namespace FCG.Games.Service.Interfaces
{
    /// <summary>
    /// Interface para operações relacionadas ao estoque de jogos.
    /// </summary>
    public interface IStockService
    {
        /// <summary>
        /// Adiciona quantidade ao estoque de um jogo.
        /// Se não existir registro de estoque para o jogo, cria um novo.
        /// </summary>
        /// <param name="addStockRequestDto">Dados da requisição de adição ao estoque.</param>
        /// <returns>Informações do estoque atualizado ou null em caso de erro.</returns>
        Task<StockResponseDto?> AddStockAsync(AddStockRequestDto addStockRequestDto);

        /// <summary>
        /// Subtrai quantidade do estoque de um jogo após uma venda.
        /// Valida se há estoque suficiente antes de realizar a operação.
        /// </summary>
        /// <param name="subStockRequestDto">Dados da requisição de subtração do estoque.</param>
        /// <returns>Informações do estoque atualizado ou null em caso de erro.</returns>
        Task<StockResponseDto?> SubStockAsync(SubStockRequestDto subStockRequestDto);

        /// <summary>
        /// Obtém informações do estoque de um jogo específico.
        /// </summary>
        /// <param name="gameId">ID do jogo.</param>
        /// <returns>Informações do estoque do jogo ou null se não encontrado.</returns>
        Task<StockResponseDto?> GetStockByGameIdAsync(long gameId);

        /// <summary>
        /// Obtém todos os registros de estoque.
        /// </summary>
        /// <returns>Lista com todos os registros de estoque.</returns>
        Task<IEnumerable<StockResponseDto>> GetAllStocksAsync();
    }
}