namespace GeocodeCache.Lambda.Observability;

/// <summary>Emits custom metrics for the geocode endpoint.</summary>
public interface IMetricsEmitter
{
    /// <summary>Records the outcome of a request as CloudWatch metrics.</summary>
    /// <param name="cacheHit">True if the response was served from cache.</param>
    /// <param name="googleStatus">The Google status, used as a metric dimension.</param>
    /// <param name="latencyMs">End-to-end handler latency in milliseconds.</param>
    void RecordRequest(bool cacheHit, string googleStatus, double latencyMs);
}
