using System.Diagnostics.CodeAnalysis;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using GeocodeCache.Application;
using GeocodeCache.Infrastructure;
using GeocodeCache.Lambda.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeocodeCache.Lambda;

/// <summary>
/// Builds the dependency-injection container once per cold start. Configuration is sourced from
/// <c>appsettings.json</c> (non-secret defaults) plus environment variables (set by Terraform),
/// so secrets are never embedded in the package.
/// </summary>
public static class Bootstrap
{
    /// <summary>Builds and returns the configured service provider.</summary>
    public static IServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        EnableXRayWhenRunningInLambda();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            // JSON console logs are captured by the Lambda runtime into CloudWatch Logs as structured entries.
            builder.AddJsonConsole(options => options.IncludeScopes = true);
        });

        services.AddApplication(configuration);
        services.AddInfrastructure(configuration);

        services.AddSingleton<IMetricsEmitter, EmfMetricsEmitter>();
        services.AddScoped<GeocodeRequestHandler>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Registers AWS X-Ray instrumentation for AWS SDK calls when executing inside Lambda.
    /// Best-effort: a failure here must never prevent the function from serving requests.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Tracing is optional; any setup failure is logged via stderr and ignored.")]
    private static void EnableXRayWhenRunningInLambda()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")))
        {
            return;
        }

        try
        {
            AWSSDKHandler.RegisterXRayForAllServices();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"X-Ray registration skipped: {ex.Message}");
        }
    }
}
