using GovAmbiental.API.Models;

namespace GovAmbiental.API.ViewModels;

/// <summary>Filtros da listagem paginada de auditorias.</summary>
public class AuditoriaFiltro
{
    public StatusAuditoria? Status { get; set; }
    public string? UnidadeOperacional { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
}

public class CriarAuditoriaRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string UnidadeOperacional { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }

    /// <summary>Ids das normas cujas requisitos serão verificados na auditoria.</summary>
    public List<int> NormaIds { get; set; } = new();
}

/// <summary>Avaliação de um requisito durante a auditoria.</summary>
public class ItemAvaliacaoRequest
{
    public int RequisitoNormaId { get; set; }
    public bool Atendido { get; set; }
    public string? Observacao { get; set; }
    public string? Evidencia { get; set; }
}

/// <summary>Conjunto de avaliações submetido ao endpoint de avaliação automática.</summary>
public class AvaliacaoAuditoriaRequest
{
    public List<ItemAvaliacaoRequest> Itens { get; set; } = new();
}

public class AuditoriaResumoResponse
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string UnidadeOperacional { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime? DataConclusao { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ScoreConformidade { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public int TotalNaoConformidades { get; set; }
}

/// <summary>Resultado consolidado da avaliação automática.</summary>
public class AvaliacaoResultadoResponse
{
    public int AuditoriaId { get; set; }
    public decimal ScoreConformidade { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public int RequisitosAvaliados { get; set; }
    public int RequisitosAtendidos { get; set; }
    public List<NaoConformidadeResponse> NaoConformidadesGeradas { get; set; } = new();
}

public class CategoriaScoreResponse
{
    public string Categoria { get; set; } = string.Empty;
    public int TotalRequisitos { get; set; }
    public int Atendidos { get; set; }
    public decimal PercentualConformidade { get; set; }
}

/// <summary>Relatório consolidado da auditoria, com quebra de score por categoria.</summary>
public class RelatorioAuditoriaResponse
{
    public int AuditoriaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string UnidadeOperacional { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ScoreConformidade { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public int TotalRequisitos { get; set; }
    public int RequisitosAtendidos { get; set; }
    public List<CategoriaScoreResponse> ScorePorCategoria { get; set; } = new();
    public List<NaoConformidadeResponse> NaoConformidades { get; set; } = new();
}
