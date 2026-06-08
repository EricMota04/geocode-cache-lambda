using GeocodeCache.Application.Abstractions;
using GeocodeCache.Application.Options;
using GeocodeCache.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GeocodeCache.Application;

/// <summary>Composition-root helpers for registering the application layer.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the geocode service and binds <see cref="CacheOptions"/> from configuration,
    /// validating it on startup so misconfiguration fails fast.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CacheOptions>()
            .Bind(configuration.GetSection(CacheOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IGeocodeService, GeocodeService>();
        return services;
    }
}
