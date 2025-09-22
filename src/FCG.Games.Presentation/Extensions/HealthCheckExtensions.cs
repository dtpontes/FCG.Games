using Microsoft.Extensions.Diagnostics.HealthChecks;
using FCG.Games.Infrastructure;

namespace FCG.Games.Presentation.Extensions
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddSqlServer(
                    configuration.GetConnectionString("DefaultConnection")!,
                    name: "sqlserver",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "database" })
                .AddDbContextCheck<AppDbContext>(
                    name: "dbcontext",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "database" });

            return services;
        }
    }
}