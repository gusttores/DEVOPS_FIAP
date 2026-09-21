using FluentValidation;
using GovAmbiental.API.ViewModels;

namespace GovAmbiental.API.Validators;

public class CriarAuditoriaRequestValidator : AbstractValidator<CriarAuditoriaRequest>
{
    public CriarAuditoriaRequestValidator()
    {
        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("O título da auditoria é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.UnidadeOperacional)
            .NotEmpty().WithMessage("A unidade operacional é obrigatória.")
            .MaximumLength(120);

        RuleFor(x => x.Responsavel)
            .NotEmpty().WithMessage("O responsável é obrigatório.")
            .MaximumLength(120);

        RuleFor(x => x.DataInicio)
            .NotEmpty().WithMessage("A data de início é obrigatória.");

        RuleFor(x => x.NormaIds)
            .NotEmpty().WithMessage("Selecione ao menos uma norma para a auditoria.");

        RuleFor(x => x.NormaIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Há normas duplicadas na seleção.");
    }
}

public class ItemAvaliacaoRequestValidator : AbstractValidator<ItemAvaliacaoRequest>
{
    public ItemAvaliacaoRequestValidator()
    {
        RuleFor(x => x.RequisitoNormaId)
            .GreaterThan(0).WithMessage("Requisito inválido.");

        RuleFor(x => x.Observacao).MaximumLength(500);
        RuleFor(x => x.Evidencia).MaximumLength(300);
    }
}

public class AvaliacaoAuditoriaRequestValidator : AbstractValidator<AvaliacaoAuditoriaRequest>
{
    public AvaliacaoAuditoriaRequestValidator()
    {
        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("Informe as avaliações dos requisitos.");

        RuleFor(x => x.Itens)
            .Must(itens => itens.Select(i => i.RequisitoNormaId).Distinct().Count() == itens.Count)
            .WithMessage("Há requisitos avaliados em duplicidade.");

        RuleForEach(x => x.Itens).SetValidator(new ItemAvaliacaoRequestValidator());
    }
}
