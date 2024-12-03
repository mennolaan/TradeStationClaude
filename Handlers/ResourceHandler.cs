namespace TradeStation.Handlers;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradeStation.Interfaces;

public class ResourceHandler : IResourceHandler
{
    private readonly ILogger<ResourceHandler> _logger;
    private readonly ITradeStationClient _tradeStationClient;

    public ResourceHandler(
        ILogger<ResourceHandler> logger,
        ITradeStationClient tradeStationClient)
    {
        _logger = logger;
        _tradeStationClient = tradeStationClient;
    }

    public async Task<string> HandleResourceCallAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        return uri.AbsolutePath switch
        {
            "/balances" => JsonSerializer.Serialize(await _tradeStationClient.GetBalancesAsync(cancellationToken)),
            "/positions" => JsonSerializer.Serialize(await _tradeStationClient.GetPositionsAsync(cancellationToken)),
            _ => throw new ArgumentException($"Unknown resource path: {uri.AbsolutePath}")
        };
    }
}
