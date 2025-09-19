﻿using FCG.Games.Service.DTO.Request;
using FluentValidation;

namespace FCG.Games.Service.DTO.Validator
{
    public class LoginUserDtoValidator : AbstractValidator<LoginUserDto>
    {
        public LoginUserDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O e-mail é obrigatório.")
                .EmailAddress().WithMessage("Formato de e-mail inválido.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("A senha é obrigatória.")
                .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
                .Matches(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[!@#$%^&*(),.?""{}|<>]).+$")
                .WithMessage("A senha deve conter letras, números e caracteres especiais.");
        }
    }
}
