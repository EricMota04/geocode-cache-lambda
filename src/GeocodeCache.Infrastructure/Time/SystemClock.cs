using GeocodeCache.Domain.Abstractions;

namespace GeocodeCache.Infrastructure.Time;

/// <summary>The production clock, backed by the system UTC time.</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
