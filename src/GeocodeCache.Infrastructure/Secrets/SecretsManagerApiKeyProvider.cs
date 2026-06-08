using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using GeocodeCache.Application.Options;
using GeocodeCache.Domain.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeocodeCache.Infrastructure.Secrets;

/// <summary>
/// Supplies the Google API key from AWS Secrets Manager, caching it for the process lifetime.
/// For local development, a <c>GOOGLE_API_KEY</c> environment variable takes precedence and the
/// Secrets Manager call is skipped. The key is never logged.
/// </summary>
public sealed partial class SecretsManagerApiKeyProvider : IGoogleApiKeyProvider, IDisposable
{
    /// <summary>Environment variable consulted first (local development override).</summary>
    public const string EnvironmentVariableName = "GOOGLE_API_KEY";

    private readonly IAmazonSecretsManager _secrets;
    private readonly GoogleGeocodingOptions _options;
    private readonly ILogger<SecretsManagerApiKeyProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _cachedKey;

    /// <summary>Creates the provider.</summary>
    public SecretsManagerApiKeyProvider(
        IAmazonSecretsManager secrets,
        IOptions<GoogleGeocodingOptions> options,
        ILogger<SecretsManagerApiKeyProvider> logger)
    {
        _secrets = secrets;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        if (_cachedKey is not null)
        {
            return _cachedKey;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedKey is not null)
            {
                return _cachedKey;
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKeySecretName))
            {
                throw new InvalidOperationException(
                    $"No '{EnvironmentVariableName}' environment variable is set and " +
                    $"'{GoogleGeocodingOptions.SectionName}:{nameof(GoogleGeocodingOptions.ApiKeySecretName)}' is not configured.");
            }

            LogFetching(_options.ApiKeySecretName);
            var response = await _secrets.GetSecretValueAsync(
                new GetSecretValueRequest { SecretId = _options.ApiKeySecretName },
                cancellationToken).ConfigureAwait(false);

            var key = ExtractKey(response.SecretString);
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException(
                    $"Secret '{_options.ApiKeySecretName}' did not contain a usable API key.");
            }

            _cachedKey = key;
            return _cachedKey;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Accepts either a raw key string or a JSON object containing a <c>GOOGLE_API_KEY</c>/<c>apiKey</c> field.
    /// </summary>
    private static string? ExtractKey(string? secretString)
    {
        if (string.IsNullOrWhiteSpace(secretString))
        {
            return null;
        }

        var trimmed = secretString.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                foreach (var name in new[] { EnvironmentVariableName, "apiKey", "ApiKey", "key" })
                {
                    if (document.RootElement.TryGetProperty(name, out var value) &&
                        value.ValueKind == JsonValueKind.String)
                    {
                        return value.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Not JSON after all — fall back to the raw string.
            }
        }

        return trimmed;
    }

    /// <summary>Releases the internal synchronization primitive.</summary>
    public void Dispose() => _gate.Dispose();

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching Google API key from Secrets Manager secret {SecretName}")]
    private partial void LogFetching(string secretName);
}
