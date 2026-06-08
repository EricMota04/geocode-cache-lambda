namespace GeocodeCache.Domain.Models;

/// <summary>
/// The <c>status</c> values returned by the Google Geocoding API.
/// See https://developers.google.com/maps/documentation/geocoding/requests-geocoding#StatusCodes.
/// </summary>
public static class GoogleGeocodeStatus
{
    /// <summary>At least one result was returned. Cacheable.</summary>
    public const string Ok = "OK";

    /// <summary>The request was valid but returned no results (e.g. unknown address). Negatively cacheable.</summary>
    public const string ZeroResults = "ZERO_RESULTS";

    /// <summary>The request exceeded the quota. Transient — must not be cached.</summary>
    public const string OverQueryLimit = "OVER_QUERY_LIMIT";

    /// <summary>The request was denied (e.g. bad key/config). Must not be cached.</summary>
    public const string RequestDenied = "REQUEST_DENIED";

    /// <summary>The request was malformed. Must not be cached.</summary>
    public const string InvalidRequest = "INVALID_REQUEST";

    /// <summary>A server-side error occurred. Transient — must not be cached.</summary>
    public const string UnknownError = "UNKNOWN_ERROR";
}
