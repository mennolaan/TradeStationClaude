namespace TradeStation.Configuration;

public record TradeStationOptions
{
    public const string ConfigurationSection = "TradeStation";

    public required string ApiKey { get; init; }
    public required string ApiSecret { get; init; }
    public required string RefreshToken { get; init; }
    public required string AccountId { get; init; }
    public string BaseUrl { get; init; } = "https://signin.tradestation.com";
    public string ApiUrl { get; init; } = "https://api.tradestation.com/v3";
}