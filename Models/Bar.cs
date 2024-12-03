namespace TradeStation.Models;

public record Bar
{
    public required DateTime DateTime { get; init; }
    public required decimal Open { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Close { get; init; }
    public required int Volume { get; init; }
    public required DateOnly Date { get; init; }
    public string? Symbol { get; init; }
}