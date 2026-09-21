namespace GovAmbiental.API.Models;

/// <summary>
/// Norma / requisito legal ambiental (ex.: Resolução CONAMA, requisito ISO 14001)
/// usada como base para avaliação de conformidade nas auditorias.
/// </summary>
public class NormaAmbiental
{
    public int Id { get; set; }

    /// <summary>Código identificador da norma (ex.: "CONAMA 430/2011").</summary>
    public string Codigo { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public CategoriaAmbiental Categoria { get; set; }

    /// <summary>Órgão emissor (ex.: CONAMA, IBAMA, ISO).</summary>
    public string OrgaoEmissor { get; set; } = string.Empty;

    public bool Ativa { get; set; } = true;

    public DateTime DataVigencia { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>Requisitos verificáveis derivados da norma.</summary>
    public ICollection<RequisitoNorma> Requisitos { get; set; } = new List<RequisitoNorma>();
}
