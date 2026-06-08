using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using GeocodeCache.Application.Abstractions;
using GeocodeCache.Domain.Exceptions;
using GeocodeCache.Domain.Models;
using GeocodeCache.Lambda.Errors;
using GeocodeCache.Lambda.Observability;
using Microsoft.Extensions.Logging;

namespace GeocodeCache.Lambda;

/// <summary>
/// Translates an API Gateway proxy request into a geocode lookup and back into an HTTP response,
/// mapping failures to consistent status codes and recording metrics.
/// </summary>
public sealed partial class GeocodeRequestHandler
{
    private const string AddressParameter = "address";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IGeocodeService _service;
    private readonly IMetricsEmitter _metrics;
    private readonly ILogger<GeocodeRequestHandler> _logger;

    /// <summary>Creates the handler.</summary>
    public GeocodeRequestHandler(
        IGeocodeService service,
        IMetricsEmitter metrics,
        ILogger<GeocodeRequestHandler> logger)
    {
        _service = service;
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>Handles a single GET /Geocode request.</summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Top-level request handler deliberately converts any unexpected failure into an HTTP 500.")]
    public async Task<APIGatewayProxyResponse> HandleAsync(
        APIGatewayProxyRequest request,
        CancellationToken cancellationToken = default)
    {
        var address = ExtractAddress(request);
        if (string.IsNullOrWhiteSpace(address))
        {
            LogMissingAddress();
            return ErrorResponse(400, "missing_address", "Query parameter 'address' is required.");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var outcome = await _service.GetGeocodeAsync(address, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            _metrics.RecordRequest(outcome.FromCache, outcome.GoogleStatus, stopwatch.Elapsed.TotalMilliseconds);
            return SuccessResponse(outcome);
        }
        catch (ArgumentException ex)
        {
            LogInvalidAddress(ex);
            return ErrorResponse(400, "invalid_address", "The supplied address is invalid.");
        }
        catch (GeocodingProviderException ex)
        {
            LogUpstreamFailure(ex);
            return ErrorResponse(502, "upstream_error", "The geocoding provider is currently unavailable.");
        }
        catch (Exception ex)
        {
            LogUnexpected(ex);
            return ErrorResponse(500, "internal_error", "An unexpected error occurred.");
        }
    }

    private static string? ExtractAddress(APIGatewayProxyRequest request) =>
        request.QueryStringParameters is { } query && query.TryGetValue(AddressParameter, out var value)
            ? value
            : null;

    private static APIGatewayProxyResponse SuccessResponse(GeocodeOutcome outcome) => new()
    {
        StatusCode = 200,
        Body = outcome.GoogleResponseJson,
        Headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["X-Cache"] = outcome.FromCache ? "HIT" : "MISS",
        },
    };

    private static APIGatewayProxyResponse ErrorResponse(int statusCode, string code, string message) => new()
    {
        StatusCode = statusCode,
        Body = JsonSerializer.Serialize(new ApiError { Error = code, Message = message }, JsonOptions),
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
    };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request rejected: missing 'address' query parameter")]
    private partial void LogMissingAddress();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request rejected: invalid address")]
    private partial void LogInvalidAddress(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Geocoding provider failure")]
    private partial void LogUpstreamFailure(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error handling geocode request")]
    private partial void LogUnexpected(Exception exception);
}
