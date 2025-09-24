namespace FCG.Games.Service.DTO.Response
{
    /// <summary>
    /// DTO para resposta de informações de estoque.
    /// </summary>
    public class StockResponseDto
    {
        /// <summary>
        /// ID do estoque.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// ID do jogo.
        /// </summary>
        public long GameId { get; set; }

        /// <summary>
        /// Nome do jogo.
        /// </summary>
        public string GameName { get; set; } = null!;

        /// <summary>
        /// Quantidade disponível em estoque.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Data de criação do registro de estoque.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da última atualização do estoque.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}