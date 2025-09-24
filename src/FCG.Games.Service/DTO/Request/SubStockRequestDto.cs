using FCG.Games.Service.DTO.Validator;

namespace FCG.Games.Service.DTO.Request
{
    /// <summary>
    /// DTO para requisição de subtração de quantidade do estoque.
    /// </summary>
    public class SubStockRequestDto : BaseDto
    {
        /// <summary>
        /// ID do jogo para subtrair do estoque.
        /// </summary>
        public long GameId { get; set; }

        /// <summary>
        /// Quantidade a ser subtraída do estoque.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Valida os dados do DTO usando FluentValidation.
        /// </summary>
        /// <returns>True se válido, senão false.</returns>
        public override bool IsValid()
        {
            ValidationResult = new SubStockRequestValidator().Validate(this);
            return ValidationResult.IsValid;
        }
    }
}