namespace TradeStation.Models.Common;

public record InputSchema
{
    public required string Type { get; init; }
    public required Dictionary<string, PropertySchema> Properties { get; init; }
    public required IEnumerable<string> Required { get; init; }
}

public record PropertySchema
{
    public required string Type { get; init; }
    public string? Description { get; init; }
}