using System.ComponentModel.DataAnnotations;

namespace GeocodeCache.Infrastructure.Persistence;

/// <summary>Configuration for the DynamoDB cache table. Bound from the <c>DynamoDb</c> config section.</summary>
public sealed class DynamoDbOptions
{
    /// <summary>The configuration section name these options bind from.</summary>
    public const string SectionName = "DynamoDb";

    /// <summary>The DynamoDB table name that stores cached geocoding responses.</summary>
    [Required]
    [MinLength(3)]
    public string TableName { get; set; } = string.Empty;
}
