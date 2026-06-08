using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;

// Use System.Text.Json for (de)serializing the Lambda event/response payloads.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GeocodeCache.Lambda;

/// <summary>
/// The Lambda entry point. The DI container is built once per cold start and reused across warm
/// invocations; each invocation runs inside its own DI scope.
/// </summary>
public static class GeocodeFunction
{
    private static readonly IServiceProvider Services = Bootstrap.BuildServiceProvider();

    /// <summary>
    /// Handles an API Gateway proxy request for <c>GET /Geocode?address=...</c>.
    /// Handler string: <c>GeocodeCache.Lambda::GeocodeCache.Lambda.GeocodeFunction::FunctionHandlerAsync</c>.
    /// </summary>
    public static async Task<APIGatewayProxyResponse> FunctionHandlerAsync(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        await using var scope = Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<GeocodeRequestHandler>();
        return await handler.HandleAsync(request).ConfigureAwait(false);
    }
}
