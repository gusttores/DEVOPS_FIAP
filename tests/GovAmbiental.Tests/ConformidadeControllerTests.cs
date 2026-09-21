using System.Net;
using System.Net.Http.Json;
using GovAmbiental.API.ViewModels;
using Xunit;

namespace GovAmbiental.Tests;

public class ConformidadeControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ConformidadeControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetNaoConformidades_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/conformidade/nao-conformidades?pageNumber=1&pageSize=10";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica se o status code é 200
    }

    [Fact]
    public async Task GetIndicadores_SemAutenticacao_RetornaUnauthorized()
    {
        var response = await _client.GetAsync("/api/conformidade/indicadores");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetIndicadores_ComAutenticacao_RetornaHttpStatusCode200()
    {
        var client = _factory.CreateClient();
        await TestAuthHelper.AutenticarAsync(client);

        var response = await client.GetAsync("/api/conformidade/indicadores");

        response.EnsureSuccessStatusCode();
        var indicadores = await response.Content.ReadFromJsonAsync<IndicadoresResponse>();
        Assert.NotNull(indicadores);
        Assert.True(indicadores!.TotalAuditorias >= 1);
    }

    [Fact]
    public async Task GetNaoConformidades_RetornaItemSemeadoComAcao()
    {
        var response = await _client.GetAsync("/api/conformidade/nao-conformidades?pageSize=10");
        response.EnsureSuccessStatusCode();

        var resultado = await response.Content.ReadFromJsonAsync<PagedResultViewModel<NaoConformidadeResponse>>();
        Assert.NotNull(resultado);
        Assert.True(resultado!.TotalItens >= 1);
        Assert.All(resultado.Itens, nc => Assert.NotNull(nc.AcaoResponsavel));
    }

    [Fact]
    public async Task GetNaoConformidades_ApenasVencidas_NaoIncluiPrazoFuturo()
    {
        // A ação semeada vence no futuro (15 dias), então não deve aparecer como vencida.
        var response = await _client.GetAsync("/api/conformidade/nao-conformidades?apenasVencidas=true");
        response.EnsureSuccessStatusCode();

        var resultado = await response.Content.ReadFromJsonAsync<PagedResultViewModel<NaoConformidadeResponse>>();
        Assert.NotNull(resultado);
        Assert.All(resultado!.Itens, nc => Assert.True(nc.Vencida));
    }
}
