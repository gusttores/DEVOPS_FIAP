using GovAmbiental.API.Common;
using GovAmbiental.API.Services;
using GovAmbiental.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GovAmbiental.API.Controllers;

/// <summary>
/// Auditorias internas de conformidade ambiental, incluindo o motor de avaliação
/// automática que calcula o score e gera não-conformidades + planos de ação.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuditoriasController : ControllerBase
{
    private readonly IAuditoriaService _auditoriaService;

    public AuditoriasController(IAuditoriaService auditoriaService)
    {
        _auditoriaService = auditoriaService;
    }

    /// <summary>Lista auditorias de forma paginada, com filtros por status/unidade/período.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultViewModel<AuditoriaResumoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] AuditoriaFiltro filtro,
        [FromQuery] PaginationParameters paginacao,
        CancellationToken ct)
    {
        var resultado = await _auditoriaService.ListarAsync(filtro, paginacao, ct);
        return Ok(resultado);
    }

    /// <summary>Relatório consolidado da auditoria, com score por categoria ambiental.</summary>
    [HttpGet("{id:int}/relatorio")]
    [ProducesResponseType(typeof(RelatorioAuditoriaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Relatorio(int id, CancellationToken ct)
    {
        var relatorio = await _auditoriaService.ObterRelatorioAsync(id, ct);
        return Ok(relatorio);
    }

    /// <summary>Planeja uma nova auditoria a partir das normas selecionadas (endpoint crítico).</summary>
    [HttpPost]
    [Authorize(Roles = "Auditor")]
    [ProducesResponseType(typeof(AuditoriaResumoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] CriarAuditoriaRequest request, CancellationToken ct)
    {
        var auditoria = await _auditoriaService.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Relatorio), new { id = auditoria.Id }, auditoria);
    }

    /// <summary>
    /// Avalia a auditoria: calcula o score ponderado, classifica o resultado e gera
    /// automaticamente as não-conformidades com planos de ação (endpoint crítico).
    /// </summary>
    [HttpPost("{id:int}/avaliar")]
    [Authorize(Roles = "Auditor")]
    [ProducesResponseType(typeof(AvaliacaoResultadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Avaliar(int id, [FromBody] AvaliacaoAuditoriaRequest request, CancellationToken ct)
    {
        var resultado = await _auditoriaService.AvaliarAsync(id, request, ct);
        return Ok(resultado);
    }
}
