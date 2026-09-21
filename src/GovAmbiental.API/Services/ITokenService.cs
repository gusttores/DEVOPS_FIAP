using GovAmbiental.API.ViewModels;

namespace GovAmbiental.API.Services;

public interface ITokenService
{
    /// <summary>Valida as credenciais contra a configuração da aplicação.</summary>
    bool ValidarCredenciais(string email, string senha);

    /// <summary>Gera um token JWT para o auditor autenticado.</summary>
    LoginResponse GerarToken(string email);
}
