using FullStackTemplate.Core.Authentication;
using FullStackTemplate.Core.Todos;
using FullStackTemplate.Data.Auditing;
using FullStackTemplate.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FullStackTemplate.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITodoService, TodoService>();

        if (
            string.Equals(
                configuration["UseInMemoryDatabase"],
                "true",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            var inMemoryDbName =
                configuration["InMemoryDatabaseName"] ?? "FullStackTemplate_Testing";
            services.AddDbContext<FullStackTemplateDbContext>(
                (provider, options) =>
                {
                    options.UseInMemoryDatabase(inMemoryDbName);
                    options.AddInterceptors(
                        provider.GetRequiredService<AuditSaveChangesInterceptor>()
                    );
                }
            );

            return services;
        }

        var fullStackTemplateConnectionString =
            configuration.GetConnectionString("FullStackTemplate")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionString:FullStackTemplate"];

        if (
            string.IsNullOrWhiteSpace(fullStackTemplateConnectionString)
            || string.Equals(
                fullStackTemplateConnectionString,
                "REPLACE_ME",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            fullStackTemplateConnectionString =
                "Server=localhost,14333;Database=FullStackTemplate;User Id=sa;Password=FullStackTemplate_Local_Dev_1;TrustServerCertificate=True;Encrypt=True;Application Name=FullStackTemplate;";
        }

        services.AddDbContext<FullStackTemplateDbContext>(
            (provider, options) =>
            {
                options.UseSqlServer(
                    fullStackTemplateConnectionString,
                    sql => sql.EnableRetryOnFailure()
                );
                options.AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>());
            }
        );

        return services;
    }
}
