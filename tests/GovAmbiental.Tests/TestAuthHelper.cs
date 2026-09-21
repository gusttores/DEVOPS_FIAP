using System.Net.Http.Headers;
using System.Net.Http.Json;
using GovAmbiental.API.ViewModels;

namespace GovAmbiental.Tests;

/// <summary>Utilitário para autenticar nos testes que exercitam endpoints protegidos.</summary>
public static class TestAuthHelper
{
    public static async Task<string> ObterTokenAsync(HttpClient client)
    {
        var login = new LoginRequest
        {
            Email = "auditor@govambiental.com",
            Senha = "Auditor@123"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", login);
        response.EnsureSuccessStatusCode();

        var conteudo = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return conteudo!.Token;
    }

    public static async Task AutenticarAsync(HttpClient client)
    {
        var token = await ObterTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
