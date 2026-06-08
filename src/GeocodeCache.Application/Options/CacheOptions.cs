using System.ComponentModel.DataAnnotations;

namespace GeocodeCache.Application.Options;

/// <summary>Configuration for cache time-to-live behavior. Bound from the <c>Cache</c> config section.</summary>
public sealed class CacheOptions
{
    /// <summary>The configuration section name these options bind from.</summary>
    public const string SectionName = "Cache";

    /// <summary>TTL (in days) for successful (<c>OK</c>) responses. Defaults to 30 per the requirement.</summary>
    [Range(1, 3650)]
    public int TtlDays { get; set; } = 30;

    /// <summary>TTL (in days) for negative (<c>ZERO_RESULTS</c>) responses. Kept short so typos recover sooner.</summary>
    [Range(1, 3650)]
    public int NegativeTtlDays { get; set; } = 1;
}
