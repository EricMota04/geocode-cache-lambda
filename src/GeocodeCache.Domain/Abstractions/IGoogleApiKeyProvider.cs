namespace GeocodeCache.Domain.Abstractions;

/// <summary>Port that supplies the Google API key from a secure source (never hard-coded).</summary>
public interface IGoogleApiKeyProvider
{
    /// <summary>Returns the Google API key, retrieving and caching it as needed.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<string> GetApiKeyAsync(CancellationToken cancellationToken = default);
}
