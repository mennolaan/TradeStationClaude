namespace TradeStation.Models;

public record Quote
{
    public required string Symbol { get; init; }
    public required decimal Open { get; init; }
    public required decimal PreviousClose { get; init; }
    public required decimal Last { get; init; }
    public required decimal Ask { get; init; }
    public required int AskSize { get; init; }
    public required decimal Bid { get; init; }
    public required int BidSize { get; init; }
    public required decimal NetChange { get; init; }
    public required decimal NetChangePct { get; init; }
    public required decimal High52Week { get; init; }
    public required DateTime High52WeekTimestamp { get; init; }
    public required decimal Low52Week { get; init; }
    public required DateTime Low52WeekTimestamp { get; init; }
    public required int Volume { get; init; }
    public required int PreviousVolume { get; init; }
    public required decimal Close { get; init; }
    public required int DailyOpenInterest { get; init; }
    public required DateTime TradeTime { get; init; }
    public required int TickSizeTier { get; init; }
}