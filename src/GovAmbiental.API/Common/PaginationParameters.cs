namespace GovAmbiental.API.Common;

/// <summary>
/// Parâmetros de paginação recebidos via query string. Aplica um teto ao tamanho
/// da página para proteger o banco contra consultas excessivamente grandes.
/// </summary>
public class PaginationParameters
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;
    private int _pageNumber = 1;

    /// <summary>Número da página (inicia em 1).</summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>Itens por página (1 a 50).</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
    }

    public int Skip => (PageNumber - 1) * PageSize;
}
