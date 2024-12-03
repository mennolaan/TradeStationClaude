namespace TradeStation.Models.Common;

public record Tool
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required InputSchema InputSchema { get; init; }
}