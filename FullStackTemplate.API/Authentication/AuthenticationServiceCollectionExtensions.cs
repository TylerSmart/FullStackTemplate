using FullStackTemplate.Core.Authentication;
using FullStackTemplate.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace FullStackTemplate.API.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public const string LocalJwtScheme = JwtBearerDefaults.AuthenticationScheme;

    public static IServiceCollection AddFullStackTemplateAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, HttpContextUserContext>();
        services.AddMemoryCache();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = LocalJwtScheme;
                options.DefaultChallengeScheme = LocalJwtScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, DualBearerAuthenticationHandler>(
                LocalJwtScheme,
                _ => { }
            );

        services.AddAuthorization();

        return services;
    }
}
