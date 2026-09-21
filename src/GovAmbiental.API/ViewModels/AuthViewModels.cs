namespace GovAmbiental.API.ViewModels;

/// <summary>Credenciais de autenticação do auditor.</summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

/// <summary>Token JWT emitido após autenticação bem-sucedida.</summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public string TipoToken { get; set; } = "Bearer";
}
