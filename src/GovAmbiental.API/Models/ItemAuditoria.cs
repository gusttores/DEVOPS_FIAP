namespace GovAmbiental.API.Models;

/// <summary>
/// Registro de conformidade de um requisito específico dentro de uma auditoria
/// (o "registro automático de conformidade" do tema).
/// </summary>
public class ItemAuditoria
{
    public int Id { get; set; }

    public int AuditoriaId { get; set; }

    public Auditoria? Auditoria { get; set; }

    public int RequisitoNormaId { get; set; }

    public RequisitoNorma? Requisito { get; set; }

    /// <summary>Indica se o requisito foi atendido na auditoria.</summary>
    public bool Atendido { get; set; }

    public string? Observacao { get; set; }

    /// <summary>Referência da evidência coletada (documento, foto, laudo).</summary>
    public string? Evidencia { get; set; }
}
