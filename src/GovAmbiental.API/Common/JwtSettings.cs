namespace GovAmbiental.API.Common;

/// <summary>Configurações de emissão/validação do token JWT (seção "Jwt").</summary>
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; } = 60;
}

/// <summary>
/// Credencial do auditor habilitado a operar a API (seção "AuthCredentials").
/// Mantida como configuração para evitar um CRUD de usuários — fora do escopo do tema.
/// </summary>
public class AuthCredentials
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Role { get; set; } = "Auditor";
}
