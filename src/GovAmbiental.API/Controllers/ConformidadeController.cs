using GovAmbiental.API.Common;
using GovAmbiental.API.Services;
using GovAmbiental.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GovAmbiental.API.Controllers;

/// <summary>Indicadores de conformidade e gestão das não-conformidades / alertas.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConformidadeController : ControllerBase
{
    private readonly IConformidadeService _conformidadeService;

    public ConformidadeController(IConformidadeService conformidadeService)
    {
        _conformidadeService = conformidadeService;
    }

    /// <summary>Indicadores (KPIs) consolidados de conformidade ambiental (endpoint crítico).</summary>
    [HttpGet("indicadores")]
    [Authorize(Roles = "Auditor")]
    [ProducesResponseType(typeof(IndicadoresResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Indicadores(CancellationToken ct)
    {
        var indicadores = await _conformidadeService.ObterIndicadoresAsync(ct);
        return Ok(indicadores);
    }

    /// <summary>
    /// Lista paginada de não-conformidades, sinalizando ações vencidas / a vencer.
    /// Use <paramref name="apenasVencidas"/> para filtrar somente os alertas vencidos.
    /// </summary>
    [HttpGet("nao-conformidades")]
    [ProducesResponseType(typeof(PagedResultViewModel<NaoConformidadeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> NaoConformidades(
        [FromQuery] bool apenasVencidas,
        [FromQuery] PaginationParameters paginacao,
        CancellationToken ct)
    {
        var resultado = await _conformidadeService.ListarNaoConformidadesAsync(apenasVencidas, paginacao, ct);
        return Ok(resultado);
    }
}
