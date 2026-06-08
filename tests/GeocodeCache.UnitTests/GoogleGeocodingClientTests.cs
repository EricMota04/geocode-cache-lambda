using System.Net;
using FluentAssertions;
using GeocodeCache.Application.Options;
using GeocodeCache.Domain.Abstractions;
using GeocodeCache.Domain.Exceptions;
using GeocodeCache.Domain.Models;
using GeocodeCache.Infrastructure.Google;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GeocodeCache.UnitTests;

public sealed class GoogleGeocodingClientTests
{
    private const string Address = "70 Vanderbilt Ave, New York, NY 10017";

    private readonly IGoogleApiKeyProvider _keyProvider = Substitute.For<IGoogleApiKeyProvider>();

    public GoogleGeocodingClientTests() =>
        _keyProvider.GetApiKeyAsync(Arg.Any<CancellationToken>()).Returns("test-key");

    private GoogleGeocodingClient CreateSut(StubHttpMessageHandler handler) => new(
        new HttpClient(handler),
        _keyProvider,
        Options.Create(new GoogleGeocodingOptions
        {
            BaseUrl = "https://maps.googleapis.com/maps/api/geocode/json",
        }),
        NullLogger<GoogleGeocodingClient>.Instance);

    [Fact]
    public async Task Returns_full_body_and_parsed_status_on_success()
    {
        const string body = "{\"status\":\"OK\",\"results\":[{\"formatted_address\":\"70 Vanderbilt Ave\"}]}";
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, body);

        var result = await CreateSut(handler).GeocodeAsync(Address);

        result.Status.Should().Be(GoogleGeocodeStatus.Ok);
        result.RawJson.Should().Be(body);
    }

    [Fact]
    public async Task Sends_escaped_address_and_key_in_query()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, "{\"status\":\"OK\"}");

        await CreateSut(handler).GeocodeAsync(Address);

        var uri = handler.LastRequest!.RequestUri!.ToString();
        uri.Should().StartWith("https://maps.googleapis.com/maps/api/geocode/json?address=");
        uri.Should().Contain("Vanderbilt");
        uri.Should().Contain("key=test-key");
    }

    [Fact]
    public async Task Parses_zero_results_status()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, "{\"status\":\"ZERO_RESULTS\",\"results\":[]}");

        var result = await CreateSut(handler).GeocodeAsync(Address);

        result.Status.Should().Be(GoogleGeocodeStatus.ZeroResults);
    }

    [Fact]
    public async Task Throws_provider_exception_on_non_success_status()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.InternalServerError, "boom");

        var act = async () => await CreateSut(handler).GeocodeAsync(Address);

        await act.Should().ThrowAsync<GeocodingProviderException>();
    }

    [Fact]
    public async Task Throws_provider_exception_on_network_failure()
    {
        var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("connection refused"));

        var act = async () => await CreateSut(handler).GeocodeAsync(Address);

        await act.Should().ThrowAsync<GeocodingProviderException>();
    }
}
