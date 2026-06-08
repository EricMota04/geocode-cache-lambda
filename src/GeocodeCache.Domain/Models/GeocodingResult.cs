namespace GeocodeCache.Domain.Models;

/// <summary>
/// The outcome of calling the upstream geocoding provider: the verbatim response body
/// plus the parsed Google <c>status</c> field used to decide cache behavior.
/// </summary>
public sealed record GeocodingResult
{
    /// <summary>The full, unmodified provider response body (JSON), forwarded to the caller as-is.</summary>
    public required string RawJson { get; init; }

    /// <summary>The Google <c>status</c> value. See <see cref="GoogleGeocodeStatus"/>.</summary>
    public required string Status { get; init; }
}
