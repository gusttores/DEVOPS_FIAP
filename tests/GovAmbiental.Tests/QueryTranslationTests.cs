using GovAmbiental.API.Common;
using GovAmbiental.API.Data;
using GovAmbiental.API.Services;
using GovAmbiental.API.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GovAmbiental.Tests;

/// <summary>
/// Valida as consultas dos services contra um provider RELACIONAL (SQLite em memória).
/// Diferente do EF InMemory, o SQLite efetivamente traduz LINQ para SQL — o que
/// confirma que as consultas (incl. agregações/GroupBy) também rodam no SQL Server.
/// </summary>
public class QueryTranslationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public QueryTranslationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        DbSeeder.SeedAsync(_context).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ListarNormas_TraduzParaSqlERetornaPaginado()
    {
        var service = new NormaService(_context);

        var resultado = await service.ListarAsync(
            new NormaFiltro { Categoria = GovAmbiental.API.Models.CategoriaAmbiental.Agua },
            new PaginationParameters { PageNumber = 1, PageSize = 10 },
            CancellationToken.None);

        Assert.True(resultado.TotalItens >= 1);
        Assert.All(resultado.Itens, n => Assert.Equal("Agua", n.Categoria));
    }

    [Fact]
    public async Task ListarAuditorias_TraduzParaSqlERetornaPaginado()
    {
        var service = new AuditoriaService(_context);

        var resultado = await service.ListarAsync(
            new AuditoriaFiltro(),
            new PaginationParameters { PageNumber = 1, PageSize = 10 },
            CancellationToken.None);

        Assert.True(resultado.TotalItens >= 1);
    }

    [Fact]
    public async Task ObterIndicadores_AgregacoesTraduzemParaSql()
    {
        var service = new ConformidadeService(_context);

        // Exercita COUNT, AVG e GROUP BY com navegação (o trecho mais sensível à tradução).
        var indicadores = await service.ObterIndicadoresAsync(CancellationToken.None);

        Assert.True(indicadores.TotalAuditorias >= 1);
        Assert.True(indicadores.ScoreMedioConformidade > 0);
        Assert.NotNull(indicadores.NormasMaisDescumpridas);
    }

    [Fact]
    public async Task ListarNaoConformidades_TraduzFiltroDeNavegacao()
    {
        var service = new ConformidadeService(_context);

        var resultado = await service.ListarNaoConformidadesAsync(
            apenasVencidas: false,
            new PaginationParameters { PageNumber = 1, PageSize = 10 },
            CancellationToken.None);

        Assert.True(resultado.TotalItens >= 1);
    }

    [Fact]
    public async Task Relatorio_CarregaIncludesERetornaScorePorCategoria()
    {
        var service = new AuditoriaService(_context);

        var relatorio = await service.ObterRelatorioAsync(1, CancellationToken.None);

        Assert.Equal(1, relatorio.AuditoriaId);
        Assert.Contains(relatorio.ScorePorCategoria, c => c.Categoria == "Agua");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
