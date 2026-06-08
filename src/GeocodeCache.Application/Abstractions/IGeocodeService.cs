using GeocodeCache.Domain.Models;

namespace GeocodeCache.Application.Abstractions;

/// <summary>Application entry point: resolve a geocode request, using the cache when possible.</summary>
public interface IGeocodeService
{
    /// <summary>
    /// Returns the geocoding result for an address, serving from cache when a non-expired entry
    /// exists and otherwise calling the provider and caching cacheable responses.
    /// </summary>
    /// <param name="address">The address to geocode.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="address"/> is null or blank.</exception>
    Task<GeocodeOutcome> GetGeocodeAsync(string address, CancellationToken cancellationToken = default);
}
