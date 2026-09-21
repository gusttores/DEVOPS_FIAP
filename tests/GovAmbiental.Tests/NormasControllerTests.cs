using System.Net;
using System.Net.Http.Json;
using GovAmbiental.API.ViewModels;
using Xunit;

namespace GovAmbiental.Tests;

public class NormasControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NormasControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/normas?pageNumber=1&pageSize=10";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica se o status code é 200
    }

    [Fact]
    public async Task Get_RetornaResultadoPaginado()
    {
        var response = await _client.GetAsync("/api/normas?pageSize=2");
        response.EnsureSuccessStatusCode();

        var resultado = await response.Content.ReadFromJsonAsync<PagedResultViewModel<NormaResponse>>();

        Assert.NotNull(resultado);
        Assert.True(resultado!.TamanhoPagina <= 2);
        Assert.True(resultado.TotalItens >= 1);
    }

    [Fact]
    public async Task Post_SemAutenticacao_RetornaUnauthorized()
    {
        var nova = new CriarNormaRequest
        {
            Codigo = "TESTE 001",
            Titulo = "Norma de teste",
            Categoria = GovAmbiental.API.Models.CategoriaAmbiental.Ar,
            OrgaoEmissor = "TESTE",
            DataVigencia = DateTime.UtcNow,
            Requisitos = new() { new() { Descricao = "Req", Peso = 1 } }
        };

        var response = await _client.PostAsJsonAsync("/api/normas", nova);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComAutenticacao_CriaNormaERetorna201()
    {
        var client = _factory.CreateClient();
        await TestAuthHelper.AutenticarAsync(client);

        var nova = new CriarNormaRequest
        {
            Codigo = "ISO 14001:2015 - 6.1",
            Titulo = "Ações para abordar riscos e oportunidades",
            Categoria = GovAmbiental.API.Models.CategoriaAmbiental.Energia,
            OrgaoEmissor = "ISO",
            DataVigencia = DateTime.UtcNow,
            Requisitos = new()
            {
                new() { Descricao = "Levantamento de aspectos ambientais", Peso = 3, Criticidade = GovAmbiental.API.Models.Criticidade.Alta }
            }
        };

        var response = await client.PostAsJsonAsync("/api/normas", nova);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var criada = await response.Content.ReadFromJsonAsync<NormaResponse>();
        Assert.NotNull(criada);
        Assert.True(criada!.Id > 0);
        Assert.Equal(1, criada.QuantidadeRequisitos);
    }

    [Fact]
    public async Task Post_SemRequisitos_RetornaBadRequest()
    {
        var client = _factory.CreateClient();
        await TestAuthHelper.AutenticarAsync(client);

        // Sem requisitos viola a regra do validador → HTTP 400.
        var invalida = new CriarNormaRequest
        {
            Codigo = "INVALIDA 001",
            Titulo = "Sem requisitos",
            Categoria = GovAmbiental.API.Models.CategoriaAmbiental.Solo,
            OrgaoEmissor = "TESTE",
            DataVigencia = DateTime.UtcNow,
            Requisitos = new()
        };

        var response = await client.PostAsJsonAsync("/api/normas", invalida);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_FiltraPorCategoria()
    {
        var response = await _client.GetAsync("/api/normas?categoria=Agua");
        response.EnsureSuccessStatusCode();

        var resultado = await response.Content.ReadFromJsonAsync<PagedResultViewModel<NormaResponse>>();
        Assert.NotNull(resultado);
        Assert.All(resultado!.Itens, n => Assert.Equal("Agua", n.Categoria));
    }
}
