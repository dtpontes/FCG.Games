namespace FCG.Games.Service.DTO.Response
{
    /// <summary>
    /// DTO para resposta de informa��es de estoque.
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
        /// Quantidade dispon�vel em estoque.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Data de cria��o do registro de estoque.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da �ltima atualiza��o do estoque.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}