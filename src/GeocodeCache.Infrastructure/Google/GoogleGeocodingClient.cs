using System.Globalization;
using System.Text.Json;
using GeocodeCache.Application.Options;
using GeocodeCache.Domain.Abstractions;
using GeocodeCache.Domain.Exceptions;
using GeocodeCache.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeocodeCache.Infrastructure.Google;

/// <summary>
/// Calls the Google Geocoding API over a typed <see cref="HttpClient"/> (resilience configured in DI).
/// Returns the full response body verbatim plus the parsed <c>status</c>. The API key is never logged.
/// </summary>
public sealed partial class GoogleGeocodingClient : IGeocodingProvider
{
    private readonly HttpClient _http;
    private readonly IGoogleApiKeyProvider _keyProvider;
    private readonly GoogleGeocodingOptions _options;
    private readonly ILogger<GoogleGeocodingClient> _logger;

    /// <summary>Creates the client.</summary>
    public GoogleGeocodingClient(
        HttpClient http,
        IGoogleApiKeyProvider keyProvider,
        IOptions<GoogleGeocodingOptions> options,
        ILogger<GoogleGeocodingClient> logger)
    {
        _http = http;
        _keyProvider = keyProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        var apiKey = await _keyProvider.GetApiKeyAsync(cancellationToken).ConfigureAwait(false);
        var requestUri =
            $"{_options.BaseUrl}?address={Uri.EscapeDataString(address)}&key={Uri.EscapeDataString(apiKey)}";

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogTransportError(address);
            throw new GeocodingProviderException("Failed to reach the Google Geocoding API.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogTimeout(address);
            throw new GeocodingProviderException("The Google Geocoding API request timed out.", ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var code = (int)response.StatusCode;
                LogNonSuccess(address, code);
                throw new GeocodingProviderException(
                    $"The Google Geocoding API returned HTTP {code.ToString(CultureInfo.InvariantCulture)}.");
            }

            var status = ExtractStatus(body);
            LogCompleted(address, status);
            return new GeocodingResult { RawJson = body, Status = status };
        }
    }

    /// <summary>Extracts the top-level <c>status</c> field; defaults to UNKNOWN_ERROR if unparsable.</summary>
    private static string ExtractStatus(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("status", out var status) &&
                status.ValueKind == JsonValueKind.String)
            {
                return status.GetString() ?? GoogleGeocodeStatus.UnknownError;
            }
        }
        catch (JsonException)
        {
            // Fall through to the default below.
        }

        return GoogleGeocodeStatus.UnknownError;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Google geocoding completed for {Address} with status {Status}")]
    private partial void LogCompleted(string address, string status);

    [LoggerMessage(Level = LogLevel.Error, Message = "Google geocoding transport failure for {Address}")]
    private partial void LogTransportError(string address);

    [LoggerMessage(Level = LogLevel.Error, Message = "Google geocoding request timed out for {Address}")]
    private partial void LogTimeout(string address);

    [LoggerMessage(Level = LogLevel.Error, Message = "Google geocoding returned non-success HTTP {StatusCode} for {Address}")]
    private partial void LogNonSuccess(string address, int statusCode);
}
