using GovAmbiental.API.Common;
using GovAmbiental.API.ViewModels;

namespace GovAmbiental.API.Services;

public interface IConformidadeService
{
    Task<IndicadoresResponse> ObterIndicadoresAsync(CancellationToken ct);

    Task<PagedResultViewModel<NaoConformidadeResponse>> ListarNaoConformidadesAsync(
        bool apenasVencidas, PaginationParameters paginacao, CancellationToken ct);
}
