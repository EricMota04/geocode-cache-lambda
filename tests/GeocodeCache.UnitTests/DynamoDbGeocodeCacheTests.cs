using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using GeocodeCache.Domain.Models;
using GeocodeCache.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GeocodeCache.UnitTests;

public sealed class DynamoDbGeocodeCacheTests
{
    private const string TableName = "GeocodeCache";
    private const string Key = "70 vanderbilt ave, new york, ny 10017";
    private static readonly DateTimeOffset CachedAt = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAt = CachedAt.AddDays(30);

    private readonly IAmazonDynamoDB _client = Substitute.For<IAmazonDynamoDB>();

    private DynamoDbGeocodeCache CreateSut() =>
        new(_client, Options.Create(new DynamoDbOptions { TableName = TableName }));

    [Fact]
    public async Task GetAsync_returns_null_when_item_not_found()
    {
        _client.GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetItemResponse()); // Item unset => IsItemSet false

        var result = await CreateSut().GetAsync(Key);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_maps_attributes_including_epoch_ttl()
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["AddressKey"] = new() { S = Key },
            ["OriginalAddress"] = new() { S = "70 Vanderbilt Ave, New York, NY 10017" },
            ["GoogleResponse"] = new() { S = "{\"status\":\"OK\"}" },
            ["GoogleStatus"] = new() { S = GoogleGeocodeStatus.Ok },
            ["IsNegative"] = new() { BOOL = false },
            ["CachedAtUtc"] = new() { S = CachedAt.ToString("O", CultureInfo.InvariantCulture) },
            ["ExpiresAt"] = new() { N = ExpiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) },
        };
        _client.GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetItemResponse { Item = item });

        var result = await CreateSut().GetAsync(Key);

        result.Should().NotBeNull();
        result!.AddressKey.Should().Be(Key);
        result.GoogleStatus.Should().Be(GoogleGeocodeStatus.Ok);
        result.IsNegative.Should().BeFalse();
        result.ExpiresAtUtc.Should().Be(ExpiresAt);
        result.CachedAtUtc.Should().Be(CachedAt);
    }

    [Fact]
    public async Task PutAsync_writes_expected_attributes_and_epoch_ttl()
    {
        PutItemRequest? captured = null;
        _client.PutItemAsync(Arg.Do<PutItemRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new PutItemResponse());

        var entry = new CachedGeocode
        {
            AddressKey = Key,
            OriginalAddress = "70 Vanderbilt Ave, New York, NY 10017",
            GoogleResponseJson = "{\"status\":\"ZERO_RESULTS\"}",
            GoogleStatus = GoogleGeocodeStatus.ZeroResults,
            IsNegative = true,
            CachedAtUtc = CachedAt,
            ExpiresAtUtc = ExpiresAt,
        };

        await CreateSut().PutAsync(entry);

        captured.Should().NotBeNull();
        captured!.TableName.Should().Be(TableName);
        captured.Item["AddressKey"].S.Should().Be(Key);
        captured.Item["GoogleStatus"].S.Should().Be(GoogleGeocodeStatus.ZeroResults);
        captured.Item["IsNegative"].BOOL.Should().Be(true);
        captured.Item["ExpiresAt"].N.Should().Be(ExpiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
    }
}
