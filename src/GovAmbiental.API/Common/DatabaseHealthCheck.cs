using GovAmbiental.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GovAmbiental.API.Common;

/// <summary>
/// Health check de readiness: confirma que a aplicação consegue abrir conexão
/// com o banco de dados configurado. Usado pelo Docker (HEALTHCHECK), pelo
/// docker-compose (condition: service_healthy) e pelas probes do Kubernetes.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;

    public DatabaseHealthCheck(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conectado = await _context.Database.CanConnectAsync(cancellationToken);

            return conectado
                ? HealthCheckResult.Healthy("Conexão com o banco de dados estabelecida.")
                : HealthCheckResult.Unhealthy("Banco de dados inacessível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao consultar o banco de dados.", ex);
        }
    }
}
