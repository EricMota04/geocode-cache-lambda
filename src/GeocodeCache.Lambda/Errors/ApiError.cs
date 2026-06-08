using System.Text.Json.Serialization;

namespace GeocodeCache.Lambda.Errors;

/// <summary>A consistent JSON error envelope returned for non-2xx responses.</summary>
public sealed record ApiError
{
    /// <summary>A short, machine-readable error code (e.g. <c>missing_address</c>).</summary>
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    /// <summary>A human-readable description of the problem.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
