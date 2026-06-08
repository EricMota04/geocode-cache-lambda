namespace GeocodeCache.Domain.Abstractions;

/// <summary>Abstraction over the system clock so time-dependent logic (TTL/expiry) is testable.</summary>
public interface IClock
{
    /// <summary>The current UTC time.</summary>
    DateTimeOffset UtcNow { get; }
}
