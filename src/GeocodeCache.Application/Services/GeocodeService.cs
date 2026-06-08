using GeocodeCache.Application.Abstractions;
using GeocodeCache.Application.Options;
using GeocodeCache.Domain.Abstractions;
using GeocodeCache.Domain.Common;
using GeocodeCache.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeocodeCache.Application.Services;

/// <summary>
/// Orchestrates the read-through cache: check the cache, fall back to the provider on a miss or
/// expired entry, and cache cacheable responses with the appropriate TTL.
/// </summary>
public sealed partial class GeocodeService : IGeocodeService
{
    private readonly IGeocodeCache _cache;
    private readonly IGeocodingProvider _provider;
    private readonly IClock _clock;
    private readonly ILogger<GeocodeService> _logger;
    private readonly CacheOptions _options;

    /// <summary>Creates the service.</summary>
    public GeocodeService(
        IGeocodeCache cache,
        IGeocodingProvider provider,
        IClock clock,
        IOptions<CacheOptions> options,
        ILogger<GeocodeService> logger)
    {
        _cache = cache;
        _provider = provider;
        _clock = clock;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<GeocodeOutcome> GetGeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        // Throws ArgumentException for null/blank input (mapped to HTTP 400 at the edge).
        var key = AddressKey.Normalize(address);
        var now = _clock.UtcNow;

        var cached = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (cached is not null && cached.ExpiresAtUtc > now)
        {
            LogCacheHit(key, cached.GoogleStatus, cached.ExpiresAtUtc);
            return GeocodeOutcome.FromCacheHit(cached);
        }

        if (cached is not null)
        {
            LogCacheExpired(key, cached.ExpiresAtUtc);
        }
        else
        {
            LogCacheMiss(key);
        }

        var result = await _provider.GeocodeAsync(address, cancellationToken).ConfigureAwait(false);

        if (TryGetTtl(result.Status, out var ttl))
        {
            var isNegative = string.Equals(result.Status, GoogleGeocodeStatus.ZeroResults, StringComparison.Ordinal);
            var entry = new CachedGeocode
            {
                AddressKey = key,
                OriginalAddress = address,
                GoogleResponseJson = result.RawJson,
                GoogleStatus = result.Status,
                IsNegative = isNegative,
                CachedAtUtc = now,
                ExpiresAtUtc = now.Add(ttl),
            };

            await _cache.PutAsync(entry, cancellationToken).ConfigureAwait(false);
            LogCached(key, result.Status, isNegative, entry.ExpiresAtUtc);
            return GeocodeOutcome.FromFreshFetch(address, result, isNegative, entry.ExpiresAtUtc);
        }

        LogNotCached(key, result.Status);
        return GeocodeOutcome.FromFreshFetch(address, result, isNegative: false, expiresAtUtc: null);
    }

    /// <summary>
    /// Determines whether and for how long a response should be cached based on its Google status.
    /// <c>OK</c> uses the full TTL; <c>ZERO_RESULTS</c> uses the short negative TTL; everything else
    /// (quota/denied/invalid/unknown) is treated as transient and not cached.
    /// </summary>
    private bool TryGetTtl(string status, out TimeSpan ttl)
    {
        switch (status)
        {
            case GoogleGeocodeStatus.Ok:
                ttl = TimeSpan.FromDays(_options.TtlDays);
                return true;
            case GoogleGeocodeStatus.ZeroResults:
                ttl = TimeSpan.FromDays(_options.NegativeTtlDays);
                return true;
            default:
                ttl = TimeSpan.Zero;
                return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache HIT for {AddressKey} (status {Status}, expires {ExpiresAt})")]
    private partial void LogCacheHit(string addressKey, string status, DateTimeOffset expiresAt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache entry for {AddressKey} expired at {ExpiresAt}; refetching from Google")]
    private partial void LogCacheExpired(string addressKey, DateTimeOffset expiresAt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache MISS for {AddressKey}; calling Google")]
    private partial void LogCacheMiss(string addressKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cached {AddressKey} (status {Status}, negative={IsNegative}) until {ExpiresAt}")]
    private partial void LogCached(string addressKey, string status, bool isNegative, DateTimeOffset expiresAt);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Not caching {AddressKey}: Google status {Status} is transient or non-cacheable")]
    private partial void LogNotCached(string addressKey, string status);
}
