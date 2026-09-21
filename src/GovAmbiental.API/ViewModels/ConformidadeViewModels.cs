namespace GovAmbiental.API.ViewModels;

public class NaoConformidadeResponse
{
    public int Id { get; set; }
    public int AuditoriaId { get; set; }
    public string AuditoriaTitulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Criticidade { get; set; } = string.Empty;
    public DateTime IdentificadaEm { get; set; }
    public string? AcaoResponsavel { get; set; }
    public DateTime? AcaoPrazoLimite { get; set; }
    public string? AcaoStatus { get; set; }

    /// <summary>Sinaliza ação corretiva com prazo vencido e ainda não concluída.</summary>
    public bool Vencida { get; set; }

    /// <summary>Dias restantes até o prazo (negativo quando vencida).</summary>
    public int? DiasParaPrazo { get; set; }
}

public class NormaDescumpridaResponse
{
    public int NormaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public int Ocorrencias { get; set; }
}

/// <summary>Indicadores (KPIs) consolidados de conformidade ambiental.</summary>
public class IndicadoresResponse
{
    public int TotalAuditorias { get; set; }
    public int AuditoriasConcluidas { get; set; }
    public decimal ScoreMedioConformidade { get; set; }
    public int TotalNaoConformidades { get; set; }
    public int AcoesVencidas { get; set; }
    public int AcoesPendentes { get; set; }
    public List<NormaDescumpridaResponse> NormasMaisDescumpridas { get; set; } = new();
}
