namespace GeocodeCache.Domain.Models;

/// <summary>
/// A cached geocoding response. Persisted by the cache port and used to satisfy
/// subsequent requests for the same normalized address until <see cref="ExpiresAtUtc"/>.
/// </summary>
public sealed record CachedGeocode
{
    /// <summary>Normalized cache key (partition key). See <see cref="Common.AddressKey"/>.</summary>
    public required string AddressKey { get; init; }

    /// <summary>The original address text as supplied by the first caller (for diagnostics).</summary>
    public required string OriginalAddress { get; init; }

    /// <summary>The full Google response body that was cached.</summary>
    public required string GoogleResponseJson { get; init; }

    /// <summary>The Google <c>status</c> of the cached response. See <see cref="GoogleGeocodeStatus"/>.</summary>
    public required string GoogleStatus { get; init; }

    /// <summary>True when this is a negative cache entry (e.g. <c>ZERO_RESULTS</c>), held for a shorter TTL.</summary>
    public required bool IsNegative { get; init; }

    /// <summary>When the entry was written.</summary>
    public required DateTimeOffset CachedAtUtc { get; init; }

    /// <summary>
    /// When the entry expires. Read paths treat <c>ExpiresAtUtc &lt;= now</c> as a miss, so expiry is
    /// correct even if the store's background TTL deletion lags.
    /// </summary>
    public required DateTimeOffset ExpiresAtUtc { get; init; }
}
