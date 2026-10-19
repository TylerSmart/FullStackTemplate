using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FullStackTemplate.Data;

public sealed class FullStackTemplateDbContextFactory
    : IDesignTimeDbContextFactory<FullStackTemplateDbContext>
{
    public FullStackTemplateDbContext CreateDbContext(string[] args)
    {
        var startupPath = ResolveStartupPath();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(startupPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("FullStackTemplate")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionString:FullStackTemplate"];

        if (
            string.IsNullOrWhiteSpace(connectionString)
            || string.Equals(connectionString, "REPLACE_ME", StringComparison.OrdinalIgnoreCase)
        )
        {
            connectionString =
                "Server=localhost,14333;Database=FullStackTemplate;User Id=sa;Password=FullStackTemplate_Local_Dev_1;TrustServerCertificate=True;Encrypt=True;Application Name=FullStackTemplate;";
        }

        var options = new DbContextOptionsBuilder<FullStackTemplateDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new FullStackTemplateDbContext(options);
    }

    private static string ResolveStartupPath()
    {
        var current = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(current, "appsettings.json")))
            return current;

        var apiPath = Path.GetFullPath(Path.Combine(current, "..", "FullStackTemplate.API"));
        if (File.Exists(Path.Combine(apiPath, "appsettings.json")))
            return apiPath;

        throw new InvalidOperationException(
            "Could not find FullStackTemplate.API appsettings.json. Run EF Core commands with --startup-project FullStackTemplate.API."
        );
    }
}
