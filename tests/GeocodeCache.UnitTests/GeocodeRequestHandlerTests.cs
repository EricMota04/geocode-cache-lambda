using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using FluentAssertions;
using GeocodeCache.Application.Abstractions;
using GeocodeCache.Domain.Exceptions;
using GeocodeCache.Domain.Models;
using GeocodeCache.Lambda;
using GeocodeCache.Lambda.Errors;
using GeocodeCache.Lambda.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GeocodeCache.UnitTests;

public sealed class GeocodeRequestHandlerTests
{
    private const string Address = "70 Vanderbilt Ave, New York, NY 10017";

    private readonly IGeocodeService _service = Substitute.For<IGeocodeService>();
    private readonly IMetricsEmitter _metrics = Substitute.For<IMetricsEmitter>();

    private GeocodeRequestHandler CreateSut() =>
        new(_service, _metrics, NullLogger<GeocodeRequestHandler>.Instance);

    private static APIGatewayProxyRequest RequestWith(string? address)
    {
        var request = new APIGatewayProxyRequest();
        if (address is not null)
        {
            request.QueryStringParameters = new Dictionary<string, string> { ["address"] = address };
        }

        return request;
    }

    private static GeocodeOutcome Outcome(bool fromCache, string status = GoogleGeocodeStatus.Ok) => new()
    {
        Address = Address,
        GoogleResponseJson = "{\"status\":\"OK\",\"results\":[]}",
        GoogleStatus = status,
        FromCache = fromCache,
        IsNegative = false,
    };

    [Fact]
    public async Task Returns_200_with_hit_header_when_served_from_cache()
    {
        _service.GetGeocodeAsync(Address, Arg.Any<CancellationToken>()).Returns(Outcome(fromCache: true));

        var response = await CreateSut().HandleAsync(RequestWith(Address));

        response.StatusCode.Should().Be(200);
        response.Headers["X-Cache"].Should().Be("HIT");
        response.Headers["Content-Type"].Should().Be("application/json");
        response.Body.Should().Contain("results");
        _metrics.Received(1).RecordRequest(true, GoogleGeocodeStatus.Ok, Arg.Any<double>());
    }

    [Fact]
    public async Task Returns_200_with_miss_header_when_freshly_fetched()
    {
        _service.GetGeocodeAsync(Address, Arg.Any<CancellationToken>()).Returns(Outcome(fromCache: false));

        var response = await CreateSut().HandleAsync(RequestWith(Address));

        response.StatusCode.Should().Be(200);
        response.Headers["X-Cache"].Should().Be("MISS");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Returns_400_when_address_missing_or_blank(string? address)
    {
        var response = await CreateSut().HandleAsync(RequestWith(address));

        response.StatusCode.Should().Be(400);
        Deserialize(response.Body).Error.Should().Be("missing_address");
        await _service.DidNotReceive().GetGeocodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_400_when_service_rejects_address()
    {
        _service.GetGeocodeAsync(Address, Arg.Any<CancellationToken>())
            .Returns<GeocodeOutcome>(_ => throw new ArgumentException("bad"));

        var response = await CreateSut().HandleAsync(RequestWith(Address));

        response.StatusCode.Should().Be(400);
        Deserialize(response.Body).Error.Should().Be("invalid_address");
    }

    [Fact]
    public async Task Returns_502_on_provider_failure()
    {
        _service.GetGeocodeAsync(Address, Arg.Any<CancellationToken>())
            .Returns<GeocodeOutcome>(_ => throw new GeocodingProviderException("down"));

        var response = await CreateSut().HandleAsync(RequestWith(Address));

        response.StatusCode.Should().Be(502);
        Deserialize(response.Body).Error.Should().Be("upstream_error");
    }

    [Fact]
    public async Task Returns_500_on_unexpected_error()
    {
        _service.GetGeocodeAsync(Address, Arg.Any<CancellationToken>())
            .Returns<GeocodeOutcome>(_ => throw new InvalidOperationException("boom"));

        var response = await CreateSut().HandleAsync(RequestWith(Address));

        response.StatusCode.Should().Be(500);
        Deserialize(response.Body).Error.Should().Be("internal_error");
    }

    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static ApiError Deserialize(string body) =>
        JsonSerializer.Deserialize<ApiError>(body, WebJson)!;
}
