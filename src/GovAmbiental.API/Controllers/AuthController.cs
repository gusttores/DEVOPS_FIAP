using GovAmbiental.API.Services;
using GovAmbiental.API.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GovAmbiental.API.Controllers;

/// <summary>
/// Autenticação do auditor. Emite o token JWT exigido pelos endpoints críticos.
/// (Infraestrutura mínima — não há manutenção de usuários, fora do escopo do tema.)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    /// <summary>Autentica o auditor e retorna um token JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!_tokenService.ValidarCredenciais(request.Email, request.Senha))
            return Unauthorized(new { mensagem = "Credenciais inválidas." });

        return Ok(_tokenService.GerarToken(request.Email));
    }
}
