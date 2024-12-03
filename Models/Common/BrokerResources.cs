namespace TradeStation.Models.Common;

public record BrokerResources
{
    public required string Host { get; init; }
    public required IEnumerable<Resource> Resources { get; init; }
}