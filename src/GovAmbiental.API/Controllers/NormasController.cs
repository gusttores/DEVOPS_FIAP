using GovAmbiental.API.Common;
using GovAmbiental.API.Services;
using GovAmbiental.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GovAmbiental.API.Controllers;

/// <summary>Cadastro e consulta paginada de normas ambientais.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NormasController : ControllerBase
{
    private readonly INormaService _normaService;

    public NormasController(INormaService normaService)
    {
        _normaService = normaService;
    }

    /// <summary>Lista normas ambientais de forma paginada, com filtros opcionais.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultViewModel<NormaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] NormaFiltro filtro,
        [FromQuery] PaginationParameters paginacao,
        CancellationToken ct)
    {
        var resultado = await _normaService.ListarAsync(filtro, paginacao, ct);
        return Ok(resultado);
    }

    /// <summary>Cadastra uma nova norma ambiental e seus requisitos (endpoint crítico).</summary>
    [HttpPost]
    [Authorize(Roles = "Auditor")]
    [ProducesResponseType(typeof(NormaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] CriarNormaRequest request, CancellationToken ct)
    {
        var norma = await _normaService.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Listar), new { id = norma.Id }, norma);
    }
}
