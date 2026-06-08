namespace GeocodeCache.Domain.Exceptions;

/// <summary>
/// Raised when the upstream geocoding provider cannot be reached or returns a transport-level
/// failure (network error, non-success HTTP status). Maps to an HTTP 502 at the edge.
/// </summary>
public sealed class GeocodingProviderException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public GeocodingProviderException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception with a message and inner cause.</summary>
    public GeocodingProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
