using GovAmbiental.API.Models;

namespace GovAmbiental.API.ViewModels;

/// <summary>Filtros da listagem paginada de normas.</summary>
public class NormaFiltro
{
    public CategoriaAmbiental? Categoria { get; set; }
    public string? OrgaoEmissor { get; set; }
    public bool? Ativa { get; set; }
}

public class CriarRequisitoRequest
{
    public string Descricao { get; set; } = string.Empty;
    public int Peso { get; set; } = 1;
    public Criticidade Criticidade { get; set; } = Criticidade.Media;
}

public class CriarNormaRequest
{
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public CategoriaAmbiental Categoria { get; set; }
    public string OrgaoEmissor { get; set; } = string.Empty;
    public DateTime DataVigencia { get; set; }
    public List<CriarRequisitoRequest> Requisitos { get; set; } = new();
}

public class RequisitoResponse
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Peso { get; set; }
    public string Criticidade { get; set; } = string.Empty;
}

public class NormaResponse
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string OrgaoEmissor { get; set; } = string.Empty;
    public bool Ativa { get; set; }
    public DateTime DataVigencia { get; set; }
    public int QuantidadeRequisitos { get; set; }
    public List<RequisitoResponse> Requisitos { get; set; } = new();
}
