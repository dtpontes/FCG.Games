using FCG.Games.Service.DTO.Request;
using FCG.Games.Service.DTO.Response;

namespace FCG.Games.Service.Interfaces
{
    /// <summary>
    /// Interface para opera��es relacionadas ao estoque de jogos.
    /// </summary>
    public interface IStockService
    {
        /// <summary>
        /// Adiciona quantidade ao estoque de um jogo.
        /// Se n�o existir registro de estoque para o jogo, cria um novo.
        /// </summary>
        /// <param name="addStockRequestDto">Dados da requisi��o de adi��o ao estoque.</param>
        /// <returns>Informa��es do estoque atualizado ou null em caso de erro.</returns>
        Task<StockResponseDto?> AddStockAsync(AddStockRequestDto addStockRequestDto);

        /// <summary>
        /// Subtrai quantidade do estoque de um jogo ap�s uma venda.
        /// Valida se h� estoque suficiente antes de realizar a opera��o.
        /// </summary>
        /// <param name="subStockRequestDto">Dados da requisi��o de subtra��o do estoque.</param>
        /// <returns>Informa��es do estoque atualizado ou null em caso de erro.</returns>
        Task<StockResponseDto?> SubStockAsync(SubStockRequestDto subStockRequestDto);

        /// <summary>
        /// Obt�m informa��es do estoque de um jogo espec�fico.
        /// </summary>
        /// <param name="gameId">ID do jogo.</param>
        /// <returns>Informa��es do estoque do jogo ou null se n�o encontrado.</returns>
        Task<StockResponseDto?> GetStockByGameIdAsync(long gameId);

        /// <summary>
        /// Obt�m todos os registros de estoque.
        /// </summary>
        /// <returns>Lista com todos os registros de estoque.</returns>
        Task<IEnumerable<StockResponseDto>> GetAllStocksAsync();
    }
}