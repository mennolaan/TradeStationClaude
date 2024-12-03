using TradeStation.Models;

namespace TradeStation.Interfaces;

public interface ITradeStationClient
{
    Task<IEnumerable<Bar>> GetIntradayDataAsync(IEnumerable<string> symbols, IEnumerable<DateTime> dates, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bar>> GetHistoricalIntradayDataAsync(IEnumerable<string> symbols, DateTime startDate, DateTime? endDate = null, int interval = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bar>> GetCurrentDayIntradayBarsAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bar>> GetHistoricalDailyBarsAsync(string symbol, int daysBack, DateTime? lastDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bar>> GetBarsAsync(string symbol, int interval, string unit, int? barsBack = null, string? firstDate = null, string? lastDate = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Bar> StreamBarsAsync(string symbol, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Quote> StreamQuotesAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IEnumerable<Dictionary<string, object>>> OpenPositionAsync(string symbol, int size, decimal tp = 0, decimal sl = 0, string orderType = "Market", decimal? price = null, CancellationToken cancellationToken = default);
    Task ClosePositionAsync(string symbol, int size, string orderType = "Market", decimal? limitPrice = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetBalancesAsync(CancellationToken cancellationToken = default);
}
