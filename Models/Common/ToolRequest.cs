namespace TradeStation.Models.Common;
public record ToolRequest
{
    public required string Name { get; init; }
    public required Dictionary<string, object> Arguments { get; init; }
}