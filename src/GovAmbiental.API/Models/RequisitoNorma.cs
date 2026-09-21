namespace GovAmbiental.API.Models;

/// <summary>
/// Requisito verificável pertencente a uma <see cref="NormaAmbiental"/>.
/// O <see cref="Peso"/> pondera o cálculo do score de conformidade da auditoria.
/// </summary>
public class RequisitoNorma
{
    public int Id { get; set; }

    public int NormaAmbientalId { get; set; }

    public NormaAmbiental? Norma { get; set; }

    public string Descricao { get; set; } = string.Empty;

    /// <summary>Peso do requisito no cálculo do score (quanto maior, mais relevante).</summary>
    public int Peso { get; set; } = 1;

    public Criticidade Criticidade { get; set; } = Criticidade.Media;

    public ICollection<ItemAuditoria> Itens { get; set; } = new List<ItemAuditoria>();
}
