using FCG.Games.Service.DTO.Validator;

namespace FCG.Games.Service.DTO.Request
{
    /// <summary>
    /// DTO para requisi��o de subtra��o de quantidade do estoque.
    /// </summary>
    public class SubStockRequestDto : BaseDto
    {
        /// <summary>
        /// ID do jogo para subtrair do estoque.
        /// </summary>
        public long GameId { get; set; }

        /// <summary>
        /// Quantidade a ser subtra�da do estoque.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Valida os dados do DTO usando FluentValidation.
        /// </summary>
        /// <returns>True se v�lido, sen�o false.</returns>
        public override bool IsValid()
        {
            ValidationResult = new SubStockRequestValidator().Validate(this);
            return ValidationResult.IsValid;
        }
    }
}