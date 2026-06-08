using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using GeocodeCache.Application.Options;
using GeocodeCache.Domain.Abstractions;
using GeocodeCache.Infrastructure.Google;
using GeocodeCache.Infrastructure.Persistence;
using GeocodeCache.Infrastructure.Secrets;
using GeocodeCache.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace GeocodeCache.Infrastructure;

/// <summary>Composition-root helpers for registering the infrastructure layer.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers AWS clients (via the default credential/region provider chain), the cache and
    /// secret adapters, and the resilient typed HTTP client for Google. Options are validated on start.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GoogleGeocodingOptions>()
            .Bind(configuration.GetSection(GoogleGeocodingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DynamoDbOptions>()
            .Bind(configuration.GetSection(DynamoDbOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton<IClock, SystemClock>();

        // AWS service clients resolve credentials/region from the standard provider chain
        // (Lambda execution role in production; profile/env locally).
        services.AddDefaultAWSOptions(configuration.GetAWSOptions());
        services.AddAWSService<IAmazonDynamoDB>();
        services.AddAWSService<IAmazonSecretsManager>();

        services.AddSingleton<IGoogleApiKeyProvider, SecretsManagerApiKeyProvider>();
        services.AddSingleton<IGeocodeCache, DynamoDbGeocodeCache>();

        var google = configuration.GetSection(GoogleGeocodingOptions.SectionName).Get<GoogleGeocodingOptions>()
                     ?? new GoogleGeocodingOptions();

        services.AddHttpClient<IGeocodingProvider, GoogleGeocodingClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(google.TimeoutSeconds);
            })
            .AddPolicyHandler(BuildRetryPolicy(google.MaxRetries));

        return services;
    }

    /// <summary>Exponential-backoff retry on transient HTTP errors (5xx, 408, network failures).</summary>
    private static AsyncRetryPolicy<HttpResponseMessage> BuildRetryPolicy(int maxRetries) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                maxRetries,
                attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)));
}
