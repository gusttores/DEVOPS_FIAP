using System.Net;
using System.Net.Http.Json;
using GovAmbiental.API.ViewModels;
using Xunit;

namespace GovAmbiental.Tests;

public class AuditoriasControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuditoriasControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/auditorias?pageNumber=1&pageSize=10";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica se o status code é 200
    }

    [Fact]
    public async Task Avaliar_GeraScoreENaoConformidades_ComAutenticacao()
    {
        // Arrange — autentica e cria uma auditoria para a norma de água (3 requisitos).
        await TestAuthHelper.AutenticarAsync(_client);

        var criar = new CriarAuditoriaRequest
        {
            Titulo = "Auditoria de teste",
            UnidadeOperacional = "Planta Teste",
            Responsavel = "QA",
            DataInicio = DateTime.UtcNow,
            NormaIds = new() { 1 }
        };
        var criarResp = await _client.PostAsJsonAsync("/api/auditorias", criar);
        criarResp.EnsureSuccessStatusCode();
        var auditoria = await criarResp.Content.ReadFromJsonAsync<AuditoriaResumoResponse>();

        // Recupera os requisitos via relatório recém-criado.
        var relResp = await _client.GetAsync($"/api/auditorias/{auditoria!.Id}/relatorio");
        relResp.EnsureSuccessStatusCode();

        // Act — avalia: 1 requisito não atendido deve gerar 1 não-conformidade.
        var avaliacao = new AvaliacaoAuditoriaRequest
        {
            Itens = new()
            {
                new() { RequisitoNormaId = 1, Atendido = false, Observacao = "pH fora do limite" },
                new() { RequisitoNormaId = 2, Atendido = true },
                new() { RequisitoNormaId = 3, Atendido = true }
            }
        };
        var avaliarResp = await _client.PostAsJsonAsync($"/api/auditorias/{auditoria.Id}/avaliar", avaliacao);

        // Assert
        avaliarResp.EnsureSuccessStatusCode();
        var resultado = await avaliarResp.Content.ReadFromJsonAsync<AvaliacaoResultadoResponse>();
        Assert.NotNull(resultado);
        Assert.Equal(3, resultado!.RequisitosAvaliados);
        Assert.Single(resultado.NaoConformidadesGeradas);
        Assert.True(resultado.ScoreConformidade > 0 && resultado.ScoreConformidade < 100);
    }

    [Fact]
    public async Task Avaliar_TodosAtendidos_ResultaConformeSemNaoConformidades()
    {
        await TestAuthHelper.AutenticarAsync(_client);

        var criar = new CriarAuditoriaRequest
        {
            Titulo = "Auditoria 100% conforme",
            UnidadeOperacional = "Planta Teste",
            Responsavel = "QA",
            DataInicio = DateTime.UtcNow,
            NormaIds = new() { 1 }
        };
        var criarResp = await _client.PostAsJsonAsync("/api/auditorias", criar);
        criarResp.EnsureSuccessStatusCode();
        var auditoria = await criarResp.Content.ReadFromJsonAsync<AuditoriaResumoResponse>();

        var avaliacao = new AvaliacaoAuditoriaRequest
        {
            Itens = new()
            {
                new() { RequisitoNormaId = 1, Atendido = true },
                new() { RequisitoNormaId = 2, Atendido = true },
                new() { RequisitoNormaId = 3, Atendido = true }
            }
        };
        var response = await _client.PostAsJsonAsync($"/api/auditorias/{auditoria!.Id}/avaliar", avaliacao);

        response.EnsureSuccessStatusCode();
        var resultado = await response.Content.ReadFromJsonAsync<AvaliacaoResultadoResponse>();
        Assert.Equal(100m, resultado!.ScoreConformidade);
        Assert.Equal("Conforme", resultado.Resultado);
        Assert.Empty(resultado.NaoConformidadesGeradas);
    }

    [Fact]
    public async Task Avaliar_RequisitosIncompletos_RetornaUnprocessableEntity()
    {
        await TestAuthHelper.AutenticarAsync(_client);

        var criar = new CriarAuditoriaRequest
        {
            Titulo = "Auditoria incompleta",
            UnidadeOperacional = "Planta Teste",
            Responsavel = "QA",
            DataInicio = DateTime.UtcNow,
            NormaIds = new() { 1 }
        };
        var criarResp = await _client.PostAsJsonAsync("/api/auditorias", criar);
        criarResp.EnsureSuccessStatusCode();
        var auditoria = await criarResp.Content.ReadFromJsonAsync<AuditoriaResumoResponse>();

        // Falta avaliar o requisito 3 → regra de negócio → 422.
        var avaliacao = new AvaliacaoAuditoriaRequest
        {
            Itens = new()
            {
                new() { RequisitoNormaId = 1, Atendido = true },
                new() { RequisitoNormaId = 2, Atendido = true }
            }
        };
        var response = await _client.PostAsJsonAsync($"/api/auditorias/{auditoria!.Id}/avaliar", avaliacao);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Avaliar_AuditoriaInexistente_RetornaNotFound()
    {
        await TestAuthHelper.AutenticarAsync(_client);

        var avaliacao = new AvaliacaoAuditoriaRequest
        {
            Itens = new() { new() { RequisitoNormaId = 1, Atendido = true } }
        };
        var response = await _client.PostAsJsonAsync("/api/auditorias/999999/avaliar", avaliacao);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Relatorio_RetornaScorePorCategoria()
    {
        // A auditoria 1 é semeada (água). O relatório deve trazer a categoria Agua.
        var response = await _client.GetAsync("/api/auditorias/1/relatorio");
        response.EnsureSuccessStatusCode();

        var relatorio = await response.Content.ReadFromJsonAsync<RelatorioAuditoriaResponse>();
        Assert.NotNull(relatorio);
        Assert.Contains(relatorio!.ScorePorCategoria, c => c.Categoria == "Agua");
    }
}
