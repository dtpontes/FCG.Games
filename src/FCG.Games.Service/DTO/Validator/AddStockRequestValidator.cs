using FCG.Games.Service.DTO.Request;
using FluentValidation;

namespace FCG.Games.Service.DTO.Validator
{
    /// <summary>
    /// Validador para AddStockRequestDto.
    /// </summary>
    public class AddStockRequestValidator : AbstractValidator<AddStockRequestDto>
    {
        public AddStockRequestValidator()
        {
            RuleFor(x => x.GameId)
                .GreaterThan(0)
                .WithMessage("O ID do jogo deve ser maior que zero.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("A quantidade deve ser maior que zero.")
                .LessThanOrEqualTo(10000)
                .WithMessage("A quantidade não pode ser maior que 10.000 unidades.");
        }
    }
}