namespace TradeStation.Models;

public record CreateOrder
{
    public required string AccountId { get; init; }
    public required string Symbol { get; init; }
    public required int Quantity { get; init; }
    public required string OrderType { get; init; }
    public required Dictionary<string, string> TimeInForce { get; init; }
    public required string TradeAction { get; init; }
    public List<CreateOrder>? OSOs { get; init; }
    public decimal? LimitPrice { get; init; }
    public decimal? StopPrice { get; init; }

    public Dictionary<string, object> ToPayload()
    {
        var payload = new Dictionary<string, object>
        {
            ["AccountID"] = AccountId,
            ["Symbol"] = Symbol,
            ["Quantity"] = Quantity.ToString(),
            ["OrderType"] = OrderType,
            ["TimeInForce"] = TimeInForce,
            ["TradeAction"] = TradeAction
        };

        if (LimitPrice.HasValue)
            payload["LimitPrice"] = LimitPrice.Value.ToString();

        if (StopPrice.HasValue)
            payload["StopPrice"] = StopPrice.Value.ToString();

        if (OSOs?.Any() == true)
        {
            payload["OSOs"] = new[]
            {
                new
                {
                    Orders = OSOs.Select(o => o.ToPayload()),
                    Type = "OCO"
                }
            };
        }

        return payload;
    }
}