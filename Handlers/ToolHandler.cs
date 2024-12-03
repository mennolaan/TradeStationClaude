namespace TradeStation.Handlers;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradeStation.Interfaces;
using TradeStation.Extensions;
using TradeStation.Models.Common;

public class ToolHandler : IToolHandler
{
    private readonly ILogger<ToolHandler> _logger;
    private readonly ITradeStationClient _tradeStationClient;
    private const string TRADESTATION_PREFIX = "tradestation";

    public ToolHandler(
        ILogger<ToolHandler> logger,
        ITradeStationClient tradeStationClient)
    {
        _logger = logger;
        _tradeStationClient = tradeStationClient;
    }

    public async Task<IEnumerable<ToolResponse>> HandleToolCallAsync(
        string name,
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!name.IsValidToolName(TRADESTATION_PREFIX))
        {
            throw new ArgumentException($"Invalid tool name prefix. Expected: {TRADESTATION_PREFIX}");
        }

        return name switch
        {
            var n when n.EndsWith("get_bars") =>
                await HandleGetBarsAsync(arguments, cancellationToken),

            var n when n.EndsWith("place_buy_order") =>
                await HandlePlaceBuyOrderAsync(arguments, cancellationToken),

            var n when n.EndsWith("place_sell_order") =>
                await HandlePlaceSellOrderAsync(arguments, cancellationToken),

            var n when n.EndsWith("get_positions") =>
                await HandleGetPositionsAsync(cancellationToken),

            var n when n.EndsWith("get_balances") =>
                await HandleGetBalancesAsync(cancellationToken),

            _ => throw new ArgumentException($"Unknown tool name: {name}")
        };
    }

    private async Task<IEnumerable<ToolResponse>> HandleGetBarsAsync(
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            var bars = await _tradeStationClient.GetBarsAsync(
                symbol: arguments["symbol"].ToString()!,
                interval: Convert.ToInt32(arguments["interval"]),
                unit: arguments["unit"].ToString()!,
                barsBack: arguments.GetValueOrDefault("bars_back") as int?,
                firstDate: arguments.GetValueOrDefault("firstdate")?.ToString(),
                lastDate: arguments.GetValueOrDefault("lastdate")?.ToString(),
                cancellationToken: cancellationToken);

            return new[] { ToolResponseFactory.CreateTextResponse(bars) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bars data");
            throw;
        }
    }

    private async Task<IEnumerable<ToolResponse>> HandlePlaceBuyOrderAsync(
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            var orderDetails = await _tradeStationClient.OpenPositionAsync(
                symbol: arguments["symbol"].ToString()!,
                size: Convert.ToInt32(arguments["size"]),
                orderType: arguments.GetValueOrDefault("order_type")?.ToString() ?? "Market",
                price: arguments.GetValueOrDefault("price") is decimal price ? price : null,
                tp: arguments.GetValueOrDefault("take_profit") is decimal tp ? tp : 0,
                sl: arguments.GetValueOrDefault("stop_loss") is decimal sl ? sl : 0,
                cancellationToken: cancellationToken);

            return new[] { ToolResponseFactory.CreateTextResponse(orderDetails) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing buy order");
            throw;
        }
    }

    private async Task<IEnumerable<ToolResponse>> HandlePlaceSellOrderAsync(
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            await _tradeStationClient.ClosePositionAsync(
                symbol: arguments["symbol"].ToString()!,
                size: Convert.ToInt32(arguments["size"]),
                orderType: arguments.GetValueOrDefault("order_type")?.ToString() ?? "Market",
                limitPrice: arguments.GetValueOrDefault("price") is decimal price ? price : null,
                cancellationToken: cancellationToken);

            return new[]
            {
                new ToolResponse
                {
                    Type = "text",
                    Text = JsonSerializer.Serialize(new { Status = "Success" })
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing sell order");
            throw;
        }
    }

    private async Task<IEnumerable<ToolResponse>> HandleGetPositionsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var positions = await _tradeStationClient.GetPositionsAsync(cancellationToken);
            return new[] { ToolResponseFactory.CreateTextResponse(positions) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting positions");
            throw;
        }
    }

    private async Task<IEnumerable<ToolResponse>> HandleGetBalancesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var balances = await _tradeStationClient.GetBalancesAsync(cancellationToken);
            return new[] { ToolResponseFactory.CreateTextResponse(balances) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balances");
            throw;
        }
    }
}