using Microsoft.Extensions.Diagnostics.HealthChecks;
using Partilhae_Net.Data;

namespace Partilhae_Net.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public DatabaseHealthCheck(AppDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conectado = await _db.Database
                .CanConnectAsync(cancellationToken);

            return conectado
                ? HealthCheckResult.Healthy("Banco de dados disponível.")
                : HealthCheckResult.Unhealthy("Banco de dados indisponível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Falha na conexão com o banco de dados.", ex);
        }
    }
}