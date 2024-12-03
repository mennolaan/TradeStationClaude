namespace TradeStation.Models.Common;

public record ToolResponse
{
    public required string Type { get; init; }
    public required string Text { get; init; }
}