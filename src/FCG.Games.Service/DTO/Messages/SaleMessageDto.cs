namespace FCG.Games.Service.DTO.Messages
{
    /// <summary>
    /// DTO que representa uma mensagem de venda recebida de outro microservi�o.
    /// </summary>
    public class SaleMessageDto
    {
        /// <summary>
        /// ID �nico da transa��o de venda.
        /// </summary>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        /// ID do jogo vendido.
        /// </summary>
        public long GameId { get; set; }

        /// <summary>
        /// Quantidade vendida.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Data e hora da venda.
        /// </summary>
        public DateTime SaleDateTime { get; set; }

        /// <summary>
        /// ID do usu�rio que realizou a compra.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Valor total da venda.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Microservi�o de origem da mensagem.
        /// </summary>
        public string SourceService { get; set; } = string.Empty;
    }
}