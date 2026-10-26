using App.Core.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace App.Core;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
