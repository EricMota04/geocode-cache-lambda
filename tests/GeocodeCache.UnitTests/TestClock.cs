using GeocodeCache.Domain.Abstractions;

namespace GeocodeCache.UnitTests;

/// <summary>A deterministic clock for tests.</summary>
internal sealed class TestClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
