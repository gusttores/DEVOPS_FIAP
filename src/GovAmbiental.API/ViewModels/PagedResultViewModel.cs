namespace GovAmbiental.API.ViewModels;

/// <summary>
/// Envelope de resultado paginado retornado pelos endpoints de listagem.
/// Expõe metadados de navegação além dos itens da página atual.
/// </summary>
/// <typeparam name="T">Tipo do item da listagem (ViewModel).</typeparam>
public class PagedResultViewModel<T>
{
    public IReadOnlyList<T> Itens { get; init; } = Array.Empty<T>();
    public int PaginaAtual { get; init; }
    public int TamanhoPagina { get; init; }
    public int TotalItens { get; init; }
    public int TotalPaginas => TamanhoPagina == 0 ? 0 : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
    public bool TemPaginaAnterior => PaginaAtual > 1;
    public bool TemProximaPagina => PaginaAtual < TotalPaginas;

    public PagedResultViewModel() { }

    public PagedResultViewModel(IReadOnlyList<T> itens, int totalItens, int paginaAtual, int tamanhoPagina)
    {
        Itens = itens;
        TotalItens = totalItens;
        PaginaAtual = paginaAtual;
        TamanhoPagina = tamanhoPagina;
    }
}
