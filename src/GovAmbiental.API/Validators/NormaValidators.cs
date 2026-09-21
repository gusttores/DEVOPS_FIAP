using FluentValidation;
using GovAmbiental.API.ViewModels;

namespace GovAmbiental.API.Validators;

public class CriarRequisitoRequestValidator : AbstractValidator<CriarRequisitoRequest>
{
    public CriarRequisitoRequestValidator()
    {
        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("A descrição do requisito é obrigatória.")
            .MaximumLength(500);

        RuleFor(x => x.Peso)
            .InclusiveBetween(1, 10).WithMessage("O peso deve estar entre 1 e 10.");

        RuleFor(x => x.Criticidade)
            .IsInEnum().WithMessage("Criticidade inválida.");
    }
}

public class CriarNormaRequestValidator : AbstractValidator<CriarNormaRequest>
{
    public CriarNormaRequestValidator()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("O código da norma é obrigatório.")
            .MaximumLength(40);

        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.OrgaoEmissor)
            .NotEmpty().WithMessage("O órgão emissor é obrigatório.")
            .MaximumLength(80);

        RuleFor(x => x.Categoria)
            .IsInEnum().WithMessage("Categoria ambiental inválida.");

        RuleFor(x => x.DataVigencia)
            .NotEmpty().WithMessage("A data de vigência é obrigatória.");

        RuleFor(x => x.Requisitos)
            .NotEmpty().WithMessage("Informe ao menos um requisito para a norma.");

        RuleForEach(x => x.Requisitos).SetValidator(new CriarRequisitoRequestValidator());
    }
}
