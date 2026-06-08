using System.ComponentModel.DataAnnotations;

namespace GeocodeCache.Application.Options;

/// <summary>Configuration for the Google Geocoding provider. Bound from the <c>Google</c> config section.</summary>
public sealed class GoogleGeocodingOptions
{
    /// <summary>The configuration section name these options bind from.</summary>
    public const string SectionName = "Google";

    /// <summary>The Geocoding API endpoint.</summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://maps.googleapis.com/maps/api/geocode/json";

    /// <summary>
    /// Name/ARN of the AWS Secrets Manager secret holding the API key. Optional when the
    /// <c>GOOGLE_API_KEY</c> environment variable is supplied (local development).
    /// </summary>
    public string ApiKeySecretName { get; set; } = string.Empty;

    /// <summary>Per-request HTTP timeout in seconds.</summary>
    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Number of retry attempts on transient HTTP failures.</summary>
    [Range(0, 5)]
    public int MaxRetries { get; set; } = 3;
}
