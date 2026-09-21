using System.Net;
using System.Net.Http.Json;
using GovAmbiental.API.ViewModels;
using Xunit;

namespace GovAmbiental.Tests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_RetornaHttpStatusCode200()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "auditor@govambiental.com",
            Senha = "Auditor@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica se o status code é 200
    }

    [Fact]
    public async Task Login_ComCredenciaisInvalidas_RetornaUnauthorized()
    {
        var request = new LoginRequest { Email = "auditor@govambiental.com", Senha = "senha-errada" };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ComEmailInvalido_RetornaBadRequest()
    {
        // Dispara a validação (FluentValidation) → HTTP 400.
        var request = new LoginRequest { Email = "nao-eh-email", Senha = "" };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
