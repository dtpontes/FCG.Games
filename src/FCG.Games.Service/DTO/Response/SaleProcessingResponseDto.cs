namespace FCG.Games.Service.DTO.Response
{
    /// <summary>
    /// DTO de resposta para o processamento de uma venda.
    /// </summary>
    public class SaleProcessingResponseDto
    {
        /// <summary>
        /// ID da transação processada.
        /// </summary>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        /// ID do jogo processado.
        /// </summary>
        public long GameId { get; set; }

        /// <summary>
        /// Nome do jogo processado.
        /// </summary>
        public string GameName { get; set; } = string.Empty;

        /// <summary>
        /// Quantidade debitada do estoque.
        /// </summary>
        public int ProcessedQuantity { get; set; }

        /// <summary>
        /// Quantidade restante em estoque após o débito.
        /// </summary>
        public int RemainingStock { get; set; }

        /// <summary>
        /// Indica se o processamento foi bem-sucedido.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Mensagem detalhando o resultado do processamento.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Data e hora do processamento.
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Lista de erros, se houver.
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }
}