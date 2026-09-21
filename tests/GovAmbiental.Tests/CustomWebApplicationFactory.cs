using GovAmbiental.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace GovAmbiental.Tests;

/// <summary>
/// Sobe a API em memória para os testes de integração, substituindo o provider
/// SQL Server por EF Core InMemory e semeando dados — roda sem SQL Server instalado.
/// Cada instância (uma por classe de teste) usa um banco isolado.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Nome + root próprios garantem que o seed e a app compartilhem o MESMO store,
    // e que cada classe de teste tenha seu banco isolado (sem interferência paralela).
    private readonly string _dbName = $"GovAmbientalTestDb_{Guid.NewGuid()}";
    private readonly InMemoryDatabaseRoot _root = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove o registro do DbContext configurado para SQL Server.
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                (d.ServiceType.FullName?.Contains("DbContextOptionsConfiguration") ?? false))
                .ToList();
            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName, _root));

            // Semeia os dados de teste no mesmo store usado pela aplicação.
            using var scope = services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.EnsureCreated();
            DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
        });
    }
}
