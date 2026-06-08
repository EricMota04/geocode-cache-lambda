using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GeocodeCache.Domain.Abstractions;
using GeocodeCache.Domain.Models;
using Microsoft.Extensions.Options;

namespace GeocodeCache.Infrastructure.Persistence;

/// <summary>
/// DynamoDB-backed implementation of <see cref="IGeocodeCache"/>. Uses the low-level client so the
/// table name is configuration-driven and the epoch-seconds TTL attribute is written explicitly.
/// </summary>
public sealed class DynamoDbGeocodeCache : IGeocodeCache
{
    // Attribute names (also referenced by the Terraform table definition and TTL config).
    internal const string AttrAddressKey = "AddressKey";
    internal const string AttrOriginalAddress = "OriginalAddress";
    internal const string AttrGoogleResponse = "GoogleResponse";
    internal const string AttrGoogleStatus = "GoogleStatus";
    internal const string AttrIsNegative = "IsNegative";
    internal const string AttrCachedAt = "CachedAtUtc";
    internal const string AttrExpiresAt = "ExpiresAt"; // epoch seconds; DynamoDB TTL attribute

    private readonly IAmazonDynamoDB _client;
    private readonly string _tableName;

    /// <summary>Creates the cache adapter.</summary>
    public DynamoDbGeocodeCache(IAmazonDynamoDB client, IOptions<DynamoDbOptions> options)
    {
        _client = client;
        _tableName = options.Value.TableName;
    }

    /// <inheritdoc />
    public async Task<CachedGeocode?> GetAsync(string addressKey, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetItemAsync(
            new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    [AttrAddressKey] = new AttributeValue { S = addressKey },
                },
                ConsistentRead = false,
            },
            cancellationToken).ConfigureAwait(false);

        return response.IsItemSet ? MapToDomain(response.Item) : null;
    }

    /// <inheritdoc />
    public async Task PutAsync(CachedGeocode entry, CancellationToken cancellationToken = default)
    {
        var newExpiry = entry.ExpiresAtUtc.ToUnixTimeSeconds();

        var item = new Dictionary<string, AttributeValue>
        {
            [AttrAddressKey] = new AttributeValue { S = entry.AddressKey },
            [AttrOriginalAddress] = new AttributeValue { S = entry.OriginalAddress },
            [AttrGoogleResponse] = new AttributeValue { S = entry.GoogleResponseJson },
            [AttrGoogleStatus] = new AttributeValue { S = entry.GoogleStatus },
            [AttrIsNegative] = new AttributeValue { BOOL = entry.IsNegative },
            [AttrCachedAt] = new AttributeValue { S = entry.CachedAtUtc.ToString("O", CultureInfo.InvariantCulture) },
            [AttrExpiresAt] = new AttributeValue
            {
                N = newExpiry.ToString(CultureInfo.InvariantCulture),
            },
        };

        try
        {
            // Cache-stampede guard: under concurrent misses for the same address, only write when
            // there is no entry or ours is fresher. A slower writer's conditional check fails and is
            // safely ignored, so it cannot overwrite a newer cached response.
            await _client.PutItemAsync(
                new PutItemRequest
                {
                    TableName           = _tableName,
                    Item                = item,
                    ConditionExpression = "attribute_not_exists(#k) OR #e < :newExpiry",
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        ["#k"] = AttrAddressKey,
                        ["#e"] = AttrExpiresAt,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":newExpiry"] = new AttributeValue
                        {
                            N = newExpiry.ToString(CultureInfo.InvariantCulture),
                        },
                    },
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (ConditionalCheckFailedException)
        {
            // A concurrent writer already stored a fresher entry — nothing to do.
        }
    }

    private static CachedGeocode MapToDomain(Dictionary<string, AttributeValue> item) => new()
    {
        AddressKey = RequireString(item, AttrAddressKey),
        OriginalAddress = RequireString(item, AttrOriginalAddress),
        GoogleResponseJson = RequireString(item, AttrGoogleResponse),
        GoogleStatus = RequireString(item, AttrGoogleStatus),
        IsNegative = item.TryGetValue(AttrIsNegative, out var neg) && (neg.BOOL ?? false),
        CachedAtUtc = DateTimeOffset.Parse(
            RequireString(item, AttrCachedAt), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        ExpiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(
            long.Parse(RequireString(item, AttrExpiresAt), CultureInfo.InvariantCulture)),
    };

    private static string RequireString(Dictionary<string, AttributeValue> item, string attribute)
    {
        if (item.TryGetValue(attribute, out var value) && value.S is not null)
        {
            return value.S;
        }

        if (item.TryGetValue(attribute, out var numeric) && numeric.N is not null)
        {
            return numeric.N;
        }

        throw new InvalidOperationException(
            $"Cache item is missing required attribute '{attribute}'.");
    }
}
