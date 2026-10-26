using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace App.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
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
            DataServiceCollectionExtensions.ResolveConfiguredConnectionString(configuration)
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured in API/appsettings.Development.json."
            );

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string ResolveStartupPath()
    {
        var current = Directory.GetCurrentDirectory();
        string[] candidates =
        [
            current,
            Path.GetFullPath(Path.Combine(current, "API")),
            Path.GetFullPath(Path.Combine(current, "..", "API")),
        ];

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Could not find API/appsettings.json. Run EF Core commands with --project Data/App.Data.csproj from the repository root."
        );
    }
}
