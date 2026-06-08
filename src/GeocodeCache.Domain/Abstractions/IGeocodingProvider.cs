using GeocodeCache.Domain.Models;

namespace GeocodeCache.Domain.Abstractions;

/// <summary>Port for the upstream geocoding provider (Google Geocoding API).</summary>
public interface IGeocodingProvider
{
    /// <summary>Geocodes an address and returns the full provider response plus its status.</summary>
    /// <param name="address">The address to geocode.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="GeocodeCache.Domain.Exceptions.GeocodingProviderException">
    /// Thrown when the provider is unreachable or returns a transport-level failure.
    /// </exception>
    Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default);
}
