using System.Text.Json;
using GeocodeCache.Domain.Abstractions;

namespace GeocodeCache.Lambda.Observability;

/// <summary>
/// Emits CloudWatch Embedded Metric Format (EMF) documents to stdout. The Lambda runtime forwards
/// stdout to CloudWatch Logs, where EMF lines are automatically extracted into custom metrics —
/// no synchronous CloudWatch API calls on the request path.
/// </summary>
public sealed class EmfMetricsEmitter : IMetricsEmitter
{
    private const string MetricNamespace = "GeocodeCache";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IClock _clock;
    private readonly TextWriter _output;

    /// <summary>Creates an emitter that writes to stdout.</summary>
    public EmfMetricsEmitter(IClock clock) : this(clock, Console.Out)
    {
    }

    /// <summary>Creates an emitter that writes to the supplied writer (used by tests).</summary>
    internal EmfMetricsEmitter(IClock clock, TextWriter output)
    {
        _clock = clock;
        _output = output;
    }

    /// <inheritdoc />
    public void RecordRequest(bool cacheHit, string googleStatus, double latencyMs)
    {
        var document = new Dictionary<string, object?>
        {
            ["_aws"] = new Dictionary<string, object?>
            {
                ["Timestamp"] = _clock.UtcNow.ToUnixTimeMilliseconds(),
                ["CloudWatchMetrics"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["Namespace"] = MetricNamespace,
                        ["Dimensions"] = new object[] { new[] { "CacheResult" } },
                        ["Metrics"] = new object[]
                        {
                            new Dictionary<string, object?> { ["Name"] = "Requests", ["Unit"] = "Count" },
                            new Dictionary<string, object?> { ["Name"] = "Latency", ["Unit"] = "Milliseconds" },
                        },
                    },
                },
            },
            ["CacheResult"] = cacheHit ? "HIT" : "MISS",
            ["GoogleStatus"] = googleStatus,
            ["Requests"] = 1,
            ["Latency"] = latencyMs,
        };

        _output.WriteLine(JsonSerializer.Serialize(document, JsonOptions));
    }
}
