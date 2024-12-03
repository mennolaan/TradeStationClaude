namespace TradeStation.Services;

using Microsoft.Extensions.Logging;
using TradeStation.Interfaces;
using TradeStation.Models.Common;

public class TradeStationServer
{
    private readonly ILogger<TradeStationServer> _logger;
    private readonly ITradeStationClient _tradeStationClient;
    private readonly IResourceHandler _resourceHandler;
    private readonly IToolHandler _toolHandler;
    private const string TRADESTATION_PREFIX = "tradestation";

    public TradeStationServer(
        ILogger<TradeStationServer> logger,
        ITradeStationClient tradeStationClient,
        IResourceHandler resourceHandler,
        IToolHandler toolHandler)
    {
        _logger = logger;
        _tradeStationClient = tradeStationClient;
        _resourceHandler = resourceHandler;
        _toolHandler = toolHandler;
    }

    public IEnumerable<Resource> ListResources()
    {
        return new[]
        {
            new Resource
            {
                Uri = new Uri($"brokerage://tradestation/balances"),
                Name = "get_balances",
                Description = "Get account balances",
                MimeType = "application/json"
            },
            new Resource
            {
                Uri = new Uri($"brokerage://tradestation/positions"),
                Name = "get_positions",
                Description = "Get current account positions",
                MimeType = "application/json"
            }
        };
    }

    public IEnumerable<Tool> ListTools()
    {
        return new[]
        {
            new Tool
            {
                Name = $"{TRADESTATION_PREFIX}_get_bars",
                Description = "Get market data as ohlc bars for a symbol",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, PropertySchema>
                    {
                        ["symbol"] = new() { Type = "string" },
                        ["unit"] = new()
                        {
                            Type = "string",
                            Description = "Unit of time for the bars. Possible values are Minute, Daily, Weekly, Monthly."
                        },
                        ["interval"] = new()
                        {
                            Type = "number",
                            Description = "Interval that each bar will consist of - for minute bars, the number of minutes aggregated in a single bar."
                        },
                        ["bars_back"] = new()
                        {
                            Type = "number",
                            Description = "Number of bars back to fetch. The maximum number of intraday bars back that a user can query is 57,600. There is no limit on daily, weekly, or monthly bars. This parameter is mutually exclusive with firstdate"
                        },
                        ["firstdate"] = new()
                        {
                            Type = "string",
                            Description = "Does not have a default value. The first date formatted as YYYY-MM-DD OR YYYY-MM-DDTHH:mm:SSZ. This parameter is mutually exclusive with barsback."
                        },
                        ["lastdate"] = new()
                        {
                            Type = "string",
                            Description = "Defaults to current timestamp. The last date formatted as YYYY-MM-DD,2020-04-20T18:00:00Z"
                        }
                    },
                    Required = new[] { "symbol", "interval", "unit" }
                }
            },
            new Tool
            {
                Name = $"{TRADESTATION_PREFIX}_place_buy_order",
                Description = "Place a buy order for any symbol, returns the placed order details or an error",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, PropertySchema>
                    {
                        ["symbol"] = new()
                        {
                            Type = "string",
                            Description = "Symbol to buy"
                        },
                        ["size"] = new()
                        {
                            Type = "number",
                            Description = "Number of shares to buy"
                        },
                        ["price"] = new()
                        {
                            Type = "number",
                            Description = "Price to buy at, applies to Limit orders"
                        },
                        ["order_type"] = new()
                        {
                            Type = "string",
                            Description = "Type of order to place. Possible values are Market, Limit, StopMarket"
                        },
                        ["take_profit"] = new()
                        {
                            Type = "number",
                            Description = "Take profit price"
                        },
                        ["stop_loss"] = new()
                        {
                            Type = "number",
                            Description = "Stop loss price"
                        }
                    },
                    Required = new[] { "symbol", "size" }
                }
            },
            new Tool
            {
                Name = $"{TRADESTATION_PREFIX}_place_sell_order",
                Description = "Place a sell order for any symbol, returns the placed order details or an error",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, PropertySchema>
                    {
                        ["symbol"] = new()
                        {
                            Type = "string",
                            Description = "Symbol to sell"
                        },
                        ["size"] = new()
                        {
                            Type = "number",
                            Description = "Number of shares to sell"
                        },
                        ["order_type"] = new()
                        {
                            Type = "string",
                            Description = "Type of order to place. Possible values are Market, Limit, StopMarket"
                        },
                        ["price"] = new()
                        {
                            Type = "number",
                            Description = "Price to sell at, applies to Limit orders"
                        }
                    },
                    Required = new[] { "symbol", "size" }
                }
            },
            new Tool
            {
                Name = $"{TRADESTATION_PREFIX}_get_positions",
                Description = "Get current account positions",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, PropertySchema>(),
                    Required = Array.Empty<string>()
                }
            },
            new Tool
            {
                Name = $"{TRADESTATION_PREFIX}_get_balances",
                Description = "Get account balances",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, PropertySchema>(),
                    Required = Array.Empty<string>()
                }
            }
        };
    }

    public async Task<string> HandleResourceCallAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        return await _resourceHandler.HandleResourceCallAsync(uri, cancellationToken);
    }

    public async Task<IEnumerable<ToolResponse>> HandleToolCallAsync(
        string name,
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken = default)
    {
        return await _toolHandler.HandleToolCallAsync(name, arguments, cancellationToken);
    }
}