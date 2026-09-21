namespace GovAmbiental.API.Models;

/// <summary>
/// Não-conformidade gerada automaticamente quando um requisito não é atendido
/// durante a avaliação de uma auditoria.
/// </summary>
public class NaoConformidade
{
    public int Id { get; set; }

    public int AuditoriaId { get; set; }

    public Auditoria? Auditoria { get; set; }

    public int RequisitoNormaId { get; set; }

    public RequisitoNorma? Requisito { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public Criticidade Criticidade { get; set; }

    public DateTime IdentificadaEm { get; set; } = DateTime.UtcNow;

    /// <summary>Plano de ação corretiva associado (gerado automaticamente).</summary>
    public AcaoCorretiva? AcaoCorretiva { get; set; }
}
