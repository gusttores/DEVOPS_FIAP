namespace GovAmbiental.API.Models;

/// <summary>
/// Auditoria interna de conformidade ambiental aplicada a uma unidade operacional.
/// Após a avaliação, recebe automaticamente um score e um resultado consolidado.
/// </summary>
public class Auditoria
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string UnidadeOperacional { get; set; } = string.Empty;

    public string Responsavel { get; set; } = string.Empty;

    public DateTime DataInicio { get; set; }

    public DateTime? DataConclusao { get; set; }

    public StatusAuditoria Status { get; set; } = StatusAuditoria.Planejada;

    /// <summary>Score ponderado de conformidade (0-100), calculado na avaliação.</summary>
    public decimal ScoreConformidade { get; set; }

    public ResultadoConformidade Resultado { get; set; } = ResultadoConformidade.NaoAvaliado;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>Itens avaliados (um por requisito de norma).</summary>
    public ICollection<ItemAuditoria> Itens { get; set; } = new List<ItemAuditoria>();

    /// <summary>Não-conformidades geradas automaticamente na avaliação.</summary>
    public ICollection<NaoConformidade> NaoConformidades { get; set; } = new List<NaoConformidade>();
}
