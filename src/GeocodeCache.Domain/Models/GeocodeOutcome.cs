namespace GeocodeCache.Domain.Models;

/// <summary>
/// The result the application returns for a geocode request: the full Google JSON to forward,
/// plus metadata describing whether it came from cache.
/// </summary>
public sealed record GeocodeOutcome
{
    /// <summary>The address that was looked up (original text).</summary>
    public required string Address { get; init; }

    /// <summary>The full Google response body to return to the caller verbatim.</summary>
    public required string GoogleResponseJson { get; init; }

    /// <summary>The Google <c>status</c> value. See <see cref="GoogleGeocodeStatus"/>.</summary>
    public required string GoogleStatus { get; init; }

    /// <summary>True if served from cache (HIT); false if freshly fetched from Google (MISS).</summary>
    public required bool FromCache { get; init; }

    /// <summary>True when the response is a negative result (<c>ZERO_RESULTS</c>).</summary>
    public required bool IsNegative { get; init; }

    /// <summary>When the cached/stored entry expires, when applicable (null for non-cached responses).</summary>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>Builds an outcome from a cache hit.</summary>
    public static GeocodeOutcome FromCacheHit(CachedGeocode cached) => new()
    {
        Address = cached.OriginalAddress,
        GoogleResponseJson = cached.GoogleResponseJson,
        GoogleStatus = cached.GoogleStatus,
        FromCache = true,
        IsNegative = cached.IsNegative,
        ExpiresAtUtc = cached.ExpiresAtUtc,
    };

    /// <summary>Builds an outcome from a fresh upstream fetch.</summary>
    public static GeocodeOutcome FromFreshFetch(
        string address,
        GeocodingResult result,
        bool isNegative,
        DateTimeOffset? expiresAtUtc) => new()
    {
        Address = address,
        GoogleResponseJson = result.RawJson,
        GoogleStatus = result.Status,
        FromCache = false,
        IsNegative = isNegative,
        ExpiresAtUtc = expiresAtUtc,
    };
}
