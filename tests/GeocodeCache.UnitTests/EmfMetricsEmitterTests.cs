using System.Text.Json;
using FluentAssertions;
using GeocodeCache.Lambda.Observability;

namespace GeocodeCache.UnitTests;

public sealed class EmfMetricsEmitterTests
{
    [Fact]
    public void RecordRequest_emits_valid_emf_document()
    {
        using var writer = new StringWriter();
        var clock = new TestClock(new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero));
        var emitter = new EmfMetricsEmitter(clock, writer);

        emitter.RecordRequest(cacheHit: true, googleStatus: "OK", latencyMs: 12.5);

        using var document = JsonDocument.Parse(writer.ToString());
        var root = document.RootElement;

        root.GetProperty("CacheResult").GetString().Should().Be("HIT");
        root.GetProperty("GoogleStatus").GetString().Should().Be("OK");
        root.GetProperty("Requests").GetInt32().Should().Be(1);
        root.GetProperty("Latency").GetDouble().Should().Be(12.5);

        var metricDirective = root.GetProperty("_aws").GetProperty("CloudWatchMetrics")[0];
        metricDirective.GetProperty("Namespace").GetString().Should().Be("GeocodeCache");
    }
}
