using FCG.Games.Service.DTO.Validator;

namespace FCG.Games.Service.DTO.Request
{
    /// <summary>
    /// DTO para requisição de adição de quantidade ao estoque.
    /// </summary>
    public class AddStockRequestDto : BaseDto
    {
        /// <summary>
        /// ID do jogo para adicionar ao estoque.
        /// </summary>
        public long GameId { get; set; }

        /// <summary>
        /// Quantidade a ser adicionada ao estoque.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Valida os dados do DTO usando FluentValidation.
        /// </summary>
        /// <returns>True se válido, senão false.</returns>
        public override bool IsValid()
        {
            ValidationResult = new AddStockRequestValidator().Validate(this);
            return ValidationResult.IsValid;
        }
    }
}