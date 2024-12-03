namespace TradeStation.Models;

public record Position
{
    public required decimal AveragePrice { get; init; }
    public required int Quantity { get; init; }
    public required string Symbol { get; init; }
    public required DateTime Timestamp { get; init; }
}