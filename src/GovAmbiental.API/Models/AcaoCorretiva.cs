namespace GovAmbiental.API.Models;

/// <summary>
/// Plano de ação corretiva para sanar uma <see cref="NaoConformidade"/>.
/// O prazo é sugerido automaticamente conforme a criticidade do requisito.
/// </summary>
public class AcaoCorretiva
{
    public int Id { get; set; }

    public int NaoConformidadeId { get; set; }

    public NaoConformidade? NaoConformidade { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public string Responsavel { get; set; } = string.Empty;

    /// <summary>Prazo limite para conclusão (calculado pela criticidade).</summary>
    public DateTime PrazoLimite { get; set; }

    public StatusAcaoCorretiva Status { get; set; } = StatusAcaoCorretiva.Pendente;
}
