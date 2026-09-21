using GovAmbiental.API.Common;
using GovAmbiental.API.ViewModels;

namespace GovAmbiental.API.Services;

public interface INormaService
{
    Task<PagedResultViewModel<NormaResponse>> ListarAsync(NormaFiltro filtro, PaginationParameters paginacao, CancellationToken ct);
    Task<NormaResponse> CriarAsync(CriarNormaRequest request, CancellationToken ct);
}
