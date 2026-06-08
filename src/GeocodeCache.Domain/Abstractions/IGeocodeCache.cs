using GeocodeCache.Domain.Models;

namespace GeocodeCache.Domain.Abstractions;

/// <summary>Persistence port for cached geocoding responses.</summary>
public interface IGeocodeCache
{
    /// <summary>
    /// Returns the cached entry for the given normalized key, or <c>null</c> if absent.
    /// Implementations may return an entry whose TTL has elapsed; callers must check expiry.
    /// </summary>
    /// <param name="addressKey">The normalized cache key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<CachedGeocode?> GetAsync(string addressKey, CancellationToken cancellationToken = default);

    /// <summary>Stores (inserts or replaces) a cache entry.</summary>
    /// <param name="entry">The entry to persist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task PutAsync(CachedGeocode entry, CancellationToken cancellationToken = default);
}
