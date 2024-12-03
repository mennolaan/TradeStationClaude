namespace TradeStation.Models.Common;

public record Resource
{
    public required Uri Uri { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string MimeType { get; init; }
}