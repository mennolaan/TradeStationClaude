// Services/TradeStationClient.cs
namespace TradeStation.Services;

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradeStation.Configuration;
using TradeStation.Extensions;
using TradeStation.Interfaces;
using TradeStation.Models;

public class TradeStationClient : ITradeStationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TradeStationClient> _logger;
    private readonly TradeStationOptions _options;
    private readonly SemaphoreSlim _tokenSemaphore;
    private DateTime? _accessTokenLastRefreshed;
    private string? _accessToken;
    private readonly HashSet<string> _ignoreSymbolList;

    public TradeStationClient(
        HttpClient httpClient,
        IOptions<TradeStationOptions> options,
        ILogger<TradeStationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _tokenSemaphore = new SemaphoreSlim(1, 1);
        _ignoreSymbolList = new HashSet<string>();

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    private async Task RefreshAccessTokenAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        if (!force && _accessTokenLastRefreshed.HasValue &&
            DateTime.UtcNow - TimeSpan.FromMinutes(10) < _accessTokenLastRefreshed.Value)
            return;

        await _tokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _options.ApiKey,
                ["client_secret"] = _options.ApiSecret,
                ["refresh_token"] = _options.RefreshToken
            };

            var response = await _httpClient.PostAsync("oauth/token",
                new FormUrlEncodedContent(tokenRequest), cancellationToken);

            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(
                cancellationToken: cancellationToken);

            _accessToken = tokenResponse!["access_token"];
            _accessTokenLastRefreshed = DateTime.UtcNow;

            _logger.LogInformation("Access token refreshed successfully");
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    private async Task<HttpResponseMessage> SendAuthorizedRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        await RefreshAccessTokenAsync(cancellationToken: cancellationToken);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<IEnumerable<Bar>> GetIntradayDataAsync(
        IEnumerable<string> symbols,
        IEnumerable<DateTime> dates,
        CancellationToken cancellationToken = default)
    {
        var tasks = symbols.Zip(dates, async (symbol, date) =>
        {
            try
            {
                var adjustedDate = date > DateTime.Now ? DateTime.Now : date;
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{_options.ApiUrl}/marketdata/barcharts/{symbol}");

                var queryParams = new Dictionary<string, string>
                {
                    ["interval"] = "5",
                    ["unit"] = "Minute",
                    ["lastdate"] = adjustedDate.ToString("yyyy-MM-dd"),
                    ["barsback"] = "78"
                };

                request.RequestUri = new Uri(request.RequestUri + "?" + new FormUrlEncodedContent(queryParams).ReadAsStringAsync());

                var response = await SendAuthorizedRequestAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
                    cancellationToken: cancellationToken);

                return ParseBarsFromResponse(data!, symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting intraday data for {Symbol}", symbol);
                return Enumerable.Empty<Bar>();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x);
    }

    public async Task<IEnumerable<Bar>> GetHistoricalIntradayDataAsync(
        IEnumerable<string> symbols,
        DateTime startDate,
        DateTime? endDate = null,
        int interval = 1,
        CancellationToken cancellationToken = default)
    {
        endDate ??= DateTime.Now;
        var maxBarsBack = 57600;
        var barsInADay = (60 * 6.5) / interval;
        var businessDays = CountBusinessDays(startDate, endDate.Value);

        if (businessDays * barsInADay > maxBarsBack)
            throw new ArgumentException("Date range too large");

        var tasks = symbols.Select(async symbol =>
        {
            if (_ignoreSymbolList.Contains(symbol))
                return Enumerable.Empty<Bar>();

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{_options.ApiUrl}/marketdata/barcharts/{symbol}");

                var queryParams = new Dictionary<string, string>
                {
                    ["interval"] = interval.ToString(),
                    ["unit"] = "Minute",
                    ["lastdate"] = endDate.Value.ToString("yyyy-MM-dd"),
                    ["firstdate"] = startDate.ToString("yyyy-MM-dd")
                };

                request.RequestUri = new Uri(request.RequestUri + "?" + new FormUrlEncodedContent(queryParams).ReadAsStringAsync());

                var response = await SendAuthorizedRequestAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
                    cancellationToken: cancellationToken);

                return ParseBarsFromResponse(data!, symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting historical intraday data for {Symbol}", symbol);
                _ignoreSymbolList.Add(symbol);
                return Enumerable.Empty<Bar>();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x);
    }

    public async Task<IEnumerable<Bar>> GetCurrentDayIntradayBarsAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.Now.Date.AddHours(9).AddMinutes(30)
                .ToUniversalTime();

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_options.ApiUrl}/marketdata/barcharts/{symbol}");

            var queryParams = new Dictionary<string, string>
            {
                ["interval"] = "5",
                ["unit"] = "Minute",
                ["firstdate"] = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            request.RequestUri = new Uri(request.RequestUri + "?" + new FormUrlEncodedContent(queryParams).ReadAsStringAsync());

            var response = await SendAuthorizedRequestAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
                cancellationToken: cancellationToken);

            return ParseBarsFromResponse(data!, symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current day intraday bars for {Symbol}", symbol);
            return Enumerable.Empty<Bar>();
        }
    }

    public async Task<IEnumerable<Bar>> GetHistoricalDailyBarsAsync(
        string symbol,
        int daysBack,
        DateTime? lastDate = null,
        CancellationToken cancellationToken = default)
    {
        lastDate ??= DateTime.Now;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_options.ApiUrl}/marketdata/barcharts/{symbol}");

            var queryParams = new Dictionary<string, string>
            {
                ["interval"] = "1",
                ["unit"] = "Daily",
                ["barsback"] = daysBack.ToString(),
                ["lastdate"] = lastDate.Value.ToString("yyyy-MM-dd")
            };

            request.RequestUri = new Uri(request.RequestUri + "?" + new FormUrlEncodedContent(queryParams).ReadAsStringAsync());

            var response = await SendAuthorizedRequestAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
                cancellationToken: cancellationToken);

            var bars = ParseBarsFromResponse(data!, symbol);

            // Filter out if last bar is too old
            if (bars.Any() && bars.Last().DateTime.Date < lastDate.Value.AddDays(-5).Date)
                return Enumerable.Empty<Bar>();

            return bars;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical daily bars for {Symbol}", symbol);
            return Enumerable.Empty<Bar>();
        }
    }

    public async Task<IEnumerable<Bar>> GetBarsAsync(
        string symbol,
        int interval,
        string unit,
        int? barsBack = null,
        string? firstDate = null,
        string? lastDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["interval"] = interval.ToString(),
            ["unit"] = unit
        };

        if ((barsBack.HasValue && !string.IsNullOrEmpty(firstDate)) ||
            (!barsBack.HasValue && string.IsNullOrEmpty(firstDate)))
            throw new ArgumentException("Either barsBack or firstDate must be provided, not both");

        if (barsBack.HasValue)
            queryParams["barsback"] = barsBack.Value.ToString();

        if (!string.IsNullOrEmpty(firstDate))
            queryParams["firstdate"] = firstDate;

        if (!string.IsNullOrEmpty(lastDate))
            queryParams["lastdate"] = lastDate;

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_options.ApiUrl}/marketdata/barcharts/{symbol}");
        request.RequestUri = new Uri(request.RequestUri + "?" + new FormUrlEncodedContent(queryParams).ReadAsStringAsync());

        var response = await SendAuthorizedRequestAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
            cancellationToken: cancellationToken);

        return ParseBarsFromResponse(data!, symbol);
    }

    public async IAsyncEnumerable<Bar> StreamBarsAsync(
        string symbol,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["interval"] = "5",
            ["unit"] = "Minute"
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_options.ApiUrl}/marketdata/stream/barcharts/{symbol}");
            request.RequestUri = new Uri(request.RequestUri + "?" + new FormUrlEncodedContent(queryParams).ReadAsStringAsync());

            var response = await SendAuthorizedRequestAsync(request, cancellationToken);

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(line);

                if (!data!.ContainsKey("High"))
                {
                    if (data.ContainsKey("Error"))
                    {
                        _logger.LogError("Error in stream: {Error}", data["Error"]);
                        break;
                    }
                    continue;
                }

                foreach (var bar in ParseBarsFromResponse(data, symbol))
                {
                    yield return bar;
                }
            }
        }
    }

    public async IAsyncEnumerable<Quote> StreamQuotesAsync(
        string symbol,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_options.ApiUrl}/marketdata/stream/quotes/{symbol}");

            var response = await SendAuthorizedRequestAsync(request, cancellationToken);

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                var quote = System.Text.Json.JsonSerializer.Deserialize<Quote>(line);

                if (quote is not null)
                    yield return quote;
            }
        }
    }

    public async Task<IEnumerable<Dictionary<string, object>>> OpenPositionAsync(
        string symbol,
        int size,
        decimal tp = 0,
        decimal sl = 0,
        string orderType = "Market",
        decimal? price = null,
        CancellationToken cancellationToken = default)
    {
        var osos = new List<CreateOrder>();

        if (tp > 0)
        {
            osos.Add(new CreateOrder
            {
                AccountId = _options.AccountId,
                Symbol = symbol.ToUpper(),
                Quantity = size,
                TradeAction = "SELL",
                OrderType = "Limit",
                TimeInForce = new Dictionary<string, string>
                {
                    ["Duration"] = IsMarketOpen() ? "DAY" : "DYP"
                },
                LimitPrice = Math.Round(tp, 2)
            });
        }

        if (sl > 0)
        {
            osos.Add(new CreateOrder
            {
                AccountId = _options.AccountId,
                Symbol = symbol.ToUpper(),
                Quantity = size,
                TradeAction = "SELL",
                OrderType = "StopMarket",
                TimeInForce = new Dictionary<string, string>
                {
                    ["Duration"] = IsMarketOpen() ? "DAY" : "DYP"
                },
                StopPrice = Math.Round(sl, 2)
            });
        }

        var createOrderBody = new CreateOrder
        {
            AccountId = _options.AccountId,
            Symbol = symbol.ToUpper(),
            Quantity = size,
            OrderType = orderType,
            TimeInForce = new Dictionary<string, string>
            {
                ["Duration"] = IsMarketOpen() ? "DAY" : "DYP"
            },
            TradeAction = "BUY",
            LimitPrice = orderType == "Limit" ? price : null,
            StopPrice = orderType == "StopMarket" ? price : null,
            OSOs = osos.Count > 0 ? osos : null
        };

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_options.ApiUrl}/orderexecution/orders")
        {
            Content = JsonContent.Create(createOrderBody.ToPayload())
        };

        var response = await SendAuthorizedRequestAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
            cancellationToken: cancellationToken);

        return ((JsonElement)result!["Orders"]).EnumerateArray()
            .Select(x => x.Deserialize<Dictionary<string, object>>()!)
            .ToList();
    }

    public async Task ClosePositionAsync(
        string symbol,
        int size,
        string orderType = "Market",
        decimal? limitPrice = null,
        CancellationToken cancellationToken = default)
    {
        var order = new CreateOrder
        {
            AccountId = _options.AccountId,
            Symbol = symbol,
            Quantity = size,
            OrderType = orderType,
            TimeInForce = new Dictionary<string, string> { ["Duration"] = "Day" },
            TradeAction = "SELL",
            LimitPrice = orderType == "Limit" ? limitPrice : null
        };

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_options.ApiUrl}/orderexecution/orders")
        {
            Content = JsonContent.Create(order.ToPayload())
        };

        var response = await SendAuthorizedRequestAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<Position>> GetPositionsAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_options.ApiUrl}/brokerage/accounts/{_options.AccountId}/positions");

        var response = await SendAuthorizedRequestAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
            cancellationToken: cancellationToken);

        return ((JsonElement)result!["Positions"]).EnumerateArray()
            .Select(x => x.Deserialize<Position>()!)
            .ToList();
    }

    public async Task<Dictionary<string, object>> GetBalancesAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_options.ApiUrl}/brokerage/accounts/{_options.AccountId}/balances");

        var response = await SendAuthorizedRequestAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
            cancellationToken: cancellationToken);

        return ((JsonElement)result!["Balances"]).Deserialize<Dictionary<string, object>>()!;
    }

    private static IEnumerable<Bar> ParseBarsFromResponse(Dictionary<string, object> response, string symbol = "")
    {
        if (!response.ContainsKey("Bars"))
            return ParseSingleBar(response, symbol).Yield();

        var barsElement = (JsonElement)response["Bars"];
        return barsElement.EnumerateArray()
            .Select(bar => ParseSingleBar(
                bar.Deserialize<Dictionary<string, object>>()!,
                symbol))
            .ToList();
    }

    private static Bar ParseSingleBar(Dictionary<string, object> barData, string symbol)
    {
        var timestamp = DateTime.Parse(barData["TimeStamp"].ToString()!);
        return new Bar
        {
            DateTime = timestamp,
            Open = decimal.Parse(barData["Open"].ToString()!),
            High = decimal.Parse(barData["High"].ToString()!),
            Low = decimal.Parse(barData["Low"].ToString()!),
            Close = decimal.Parse(barData["Close"].ToString()!),
            Volume = int.Parse(barData["TotalVolume"].ToString()!),
            Date = DateOnly.FromDateTime(timestamp),
            Symbol = string.IsNullOrEmpty(symbol) ? null : symbol
        };
    }

    private static bool IsMarketOpen()
    {
        var now = DateTime.Now;
        if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;

        var marketOpenTime = now.Date.AddHours(9).AddMinutes(30);
        var marketCloseTime = now.Date.AddHours(16);

        return now >= marketOpenTime && now <= marketCloseTime;
    }

    private static int CountBusinessDays(DateTime startDate, DateTime endDate)
    {
        int days = 0;
        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                days++;
        }
        return days;
    }
}