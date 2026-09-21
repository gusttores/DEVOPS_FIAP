using GovAmbiental.API.Common;
using GovAmbiental.API.ViewModels;

namespace GovAmbiental.API.Services;

public interface IAuditoriaService
{
    Task<PagedResultViewModel<AuditoriaResumoResponse>> ListarAsync(
        AuditoriaFiltro filtro, PaginationParameters paginacao, CancellationToken ct);

    Task<AuditoriaResumoResponse> CriarAsync(CriarAuditoriaRequest request, CancellationToken ct);

    /// <summary>
    /// Recebe as avaliações dos requisitos, calcula o score ponderado, classifica o
    /// resultado e gera automaticamente as não-conformidades com planos de ação.
    /// </summary>
    Task<AvaliacaoResultadoResponse> AvaliarAsync(int auditoriaId, AvaliacaoAuditoriaRequest request, CancellationToken ct);

    Task<RelatorioAuditoriaResponse> ObterRelatorioAsync(int auditoriaId, CancellationToken ct);
}
