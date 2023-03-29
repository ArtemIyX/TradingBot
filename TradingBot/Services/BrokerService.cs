using Binance.Net.Clients;
using Binance.Net.Objects;
using Bybit.Net.Clients;
using Bybit.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Globalization;
using TradingBot.Data;

namespace TradingBot.Services;

internal class BrokerService
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookService> _logger;
    private readonly ExchangeServiceConfig _exchangeServiceConfig;
    private readonly BotConfig _botConfig;
    private readonly BinanceClient _binanceClient;
    private readonly BybitClient _bybitClient;

    public CryptoCurrency CurrentCurrency { get; private set; }
    public bool HasPosition { get; private set; }
    public bool Buy { get; private set; }
    public decimal Enter { get; private set; }
    public string OrderId { get; private set; }
    public DateTime OrderStarted { get; private set; }

    public event CurrencyTpSlDelegate? TpSlReached;

    public BrokerService(IConfiguration config,
        ILogger<WebhookService> logger)
    {
        OrderId = "";
        CurrentCurrency = CryptoCurrency.None;
        HasPosition = false;
        _config = config;
        _logger = logger;

        _exchangeServiceConfig = _config.GetSection("ExchangeService").Get<ExchangeServiceConfig>();
        _botConfig = _config.GetSection("Bot").Get<BotConfig>() ?? throw new InvalidOperationException("Can not get 'Bot' from settings");

        BinanceClientOptions binanceClientOption = new BinanceClientOptions
        {
            ApiCredentials = new BinanceApiCredentials(_exchangeServiceConfig.ApiKey, _exchangeServiceConfig.SecretKey)
        };

        if (_exchangeServiceConfig.TestNet)
        {
            binanceClientOption.UsdFuturesApiOptions = new BinanceApiClientOptions()
            {
                BaseAddress = "https://testnet.binancefuture.com"
            };
        }

        _binanceClient = new BinanceClient(binanceClientOption);


        BybitClientOptions bybitClientOptions = new BybitClientOptions()
        {
            ApiCredentials = new ApiCredentials(_exchangeServiceConfig.ApiKey, _exchangeServiceConfig.SecretKey)
        };
        _bybitClient = new BybitClient(bybitClientOptions);
    }

    public async Task StartMonitorCurrency(CryptoCurrency currency, bool buy, decimal take, decimal stop,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Started TPSL monitor...");
        MonitorResult result = new MonitorResult
        {
            Buy = buy,
            Currency = currency
        };
        int i = 1;
        int need = 3;

        void Print(decimal currentPrice)
        {
            Console.Clear();
            _logger.LogInformation($"Monitoring {currency}: {currentPrice} (TP: {take}, SP: {stop})");
        }

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            decimal current = await GetAvgPrice(currency);
            i++;
            if (i == need)
            {
                Print(current);
                i = 1;
            }

            if (buy)
            {
                if (current >= take)
                {
                    result.Take = true;
                    result.Price = current;
                    Print(current);
                    TpSlReached?.Invoke(result);
                    return;
                }
                else if (current <= stop)
                {
                    result.Take = false;
                    result.Price = current;
                    Print(current);
                    TpSlReached?.Invoke(result);
                    return;
                }
            }
            else
            {
                if (current <= take)
                {
                    result.Take = true;
                    result.Price = current;
                    Print(current);
                    TpSlReached?.Invoke(result);
                    return;
                }
                else if (current >= stop)
                {
                    result.Take = false;
                    result.Price = current;
                    Print(current);
                    TpSlReached?.Invoke(result);
                    return;
                }
            }

            await Task.Delay(1000, cancellationToken);
        }
    }

    // This method retrieves the available balance of USDT in a Binance futures account.
    public async Task<decimal> GetUsdtFuturesBalance()
    {
        if (_botConfig.UseBinance)
        {
            // Use the Binance .NET library to call the USDT Futures API and get account balances.
            WebCallResult<IEnumerable<Binance.Net.Objects.Models.Futures.BinanceFuturesAccountBalance>> res
                = await _binanceClient.UsdFuturesApi.Account.GetBalancesAsync();

            // If the API call was unsuccessful, throw an exception with the error message.
            if (!res.Success)
                throw new Exception(res.Error?.Message);

            // Loop through each balance to find the USDT balance and return it.
            foreach (var data in res.Data)
            {
                if (data.Asset == "USDT")
                {
                    return data.AvailableBalance;
                }
            }

            // If no USDT balance was found, throw an exception with a custom error message.
            throw new Exception("Can not find USDT Asset");
        }
        else
        {
            WebCallResult<Dictionary<string, Bybit.Net.Objects.Models.BybitBalance>> res =
                await _bybitClient.UsdPerpetualApi.Account.GetBalancesAsync();
            // If the API call was unsuccessful, throw an exception with the error message.
            if (!res.Success)
                throw new Exception(res.Error.Message);

            // Loop through each balance to find the USDT balance and return it.
            foreach (var data in res.Data)
            {
                if (data.Key == "USDT")
                {
                    return data.Value.AvailableBalance;
                }
            }

            // If no USDT balance was found, throw an exception with a custom error message.
            throw new Exception("Can not find USDT Asset");
        }
    }

    // This method asynchronously retrieves the current average price for the specified currency from the Binance API.
    // It takes a BinanceCurrency object as a parameter and returns a decimal value representing the average price.
    public async Task<decimal> GetAvgPrice(CryptoCurrency currency)
    {
        // Call the Binance API to get the current average price for the currency
        //var res = await _binanceClient.UsdFuturesApi.ExchangeData.GetPriceAsync(currency.ToString().ToUpper());
        //var res = await _binanceClient.SpotApi.ExchangeData.GetCurrentAvgPriceAsync(currency.ToString().ToUpper());
        //var res = await _binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(currency.ToString(),
        //            (KlineInterval)_binanceConfig.KlinePeriod, limit: 1);
        if (_botConfig.UseBinance)
        {
            RestClient client = new RestClient("https://fapi.binance.com");
            RestRequest request = new RestRequest("/fapi/v1/ticker/price", Method.Get);
            request.AddParameter("symbol", currency.ToString().ToUpper());

            RestResponse response = await client.ExecuteAsync(request);
            dynamic data =
                JsonConvert.DeserializeObject<dynamic>(response.Content ??
                                                       throw new Exception("Can not get Content from price request"))
                ?? throw new Exception("Can not parse json from price response");
            return data.price;
        }
        else
        {
            RestClient client = new RestClient("https://api.bybit.com/");
            RestRequest request = new RestRequest("v2/public/tickers", Method.Get);
            request.AddParameter("symbol", currency.ToString().ToUpper());
            RestResponse response = await client.ExecuteAsync(request);
            JObject jresult =
                JObject.Parse(response.Content ?? throw new Exception("Can not get Conntet from price request"));
            JArray resultArray = jresult["result"] as JArray ??
                                 throw new Exception("Can not find 'result' array in json content from price request");
            JObject result = (JObject)resultArray[0];
            string lastPrice = (result["last_price"] ?? throw new InvalidOperationException()).Value<string>() ?? throw new InvalidOperationException();
            CultureInfo culture = CultureInfo.InvariantCulture;
            decimal price = decimal.Parse(lastPrice, NumberStyles.Number, culture);
            return price;
        }
    }


    public async Task<List<BinanceCandlestickData>> GetRecentBinanceCandles(CryptoCurrency currency,
        Binance.Net.Enums.KlineInterval interval, int limit)
    {
        string symbol = currency.ToString().ToUpper();

        string inter = BinanceServiceExtensions.ConvertKlineIntervalToString(interval);

        string apiUrl = $"https://api.binance.com/api/v3/klines?symbol={symbol}&interval={inter}&limit={limit}";
        List<BinanceCandlestickData> res = new List<BinanceCandlestickData>();
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(apiUrl);

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<object[][]>(json);
            CultureInfo culture = CultureInfo.InvariantCulture;
            foreach (var candle in data)
            {
                res.Add(new BinanceCandlestickData
                {
                    OpenTime = (long)candle[0],
                    Open = decimal.Parse((string)candle[1], NumberStyles.Number, culture),
                    High = decimal.Parse((string)candle[2], NumberStyles.Number, culture),
                    Low = decimal.Parse((string)candle[3], NumberStyles.Number, culture),
                    Close = decimal.Parse((string)candle[4], NumberStyles.Number, culture),
                    Volume = decimal.Parse((string)candle[5], NumberStyles.Number, culture),
                    CloseTime = (long)candle[6]
                });
            }
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
        }

        return res;
    }

    public async Task<TakeProfitStopLossResult> CalculateTPSL_Advanced(CryptoCurrency currency, bool buy, decimal take, decimal loss,
        decimal pipSize)
    {
        try
        {
            decimal avgPprice = await GetAvgPrice(currency);
            string label = buy ? "BUY" : "SELL";
            _logger.LogInformation(
                $"Calculating advanced TPSL for {label}. Price: {avgPprice}, TP pips: {take}, SL Pips: {loss}");
            if (buy)
            {
                return new TakeProfitStopLossResult()
                {
                    Success = true,
                    Price = avgPprice,
                    Take = avgPprice + (take * pipSize),
                    Loss = avgPprice - (loss * pipSize)
                };
            }
            else
            {
                return new TakeProfitStopLossResult()
                {
                    Success = true,
                    Price = avgPprice,
                    Take = avgPprice - (take * pipSize),
                    Loss = avgPprice + (loss * pipSize)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new TakeProfitStopLossResult()
            {
                Success = false
            };
        }
    }

    public async Task<TakeProfitStopLossResult> CalculateTpSl(CryptoCurrency currency, bool buy, decimal takePercent)
    {
        try
        {
            decimal avgPprice = await GetAvgPrice(currency);
            var candles = await GetRecentBinanceCandles(currency,
                (Binance.Net.Enums.KlineInterval)_exchangeServiceConfig.KlinePeriod, _exchangeServiceConfig.SwingLen);

            decimal sw_high = candles.Max(x => x.High);
            decimal sw_low = candles.Min(x => x.Low);

            decimal buyTakeProfit = avgPprice + Math.Abs(avgPprice - sw_low) * takePercent;
            decimal sellTakeProfit = avgPprice - Math.Abs(sw_high - avgPprice) * takePercent;

            decimal actualProfit = buy ? buyTakeProfit : sellTakeProfit;
            decimal actualLoss = buy ? sw_low : sw_high;

            string label = buy ? "BUY" : "SELL";
            _logger.LogInformation(
                $"Calculating TPSL for {label}. Price: {avgPprice}, TP: {actualProfit}, SL: {actualLoss}");

            if (buy)
            {
                if (sw_low > avgPprice)
                {
                    throw new Exception(
                        $"Calculating TPSL for {label} {currency} at {avgPprice}, but recent swing low is {sw_low}");
                }
            }
            else
            {
                if (sw_high < avgPprice)
                {
                    throw new Exception(
                        $"Calculating TPSL for {label} {currency} at {avgPprice}, but recent swing high is {sw_high}");
                }
            }

            return new TakeProfitStopLossResult()
            {
                Price = avgPprice,
                Take = actualProfit,
                Loss = actualLoss,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new TakeProfitStopLossResult()
            {
                Success = false
            };
        }
    }

    // This method asynchronously attempts to cancel an open order for the specified currency and order ID.
    // It takes a BinanceCurrency object and a long integer representing the order ID as parameters.
    private async Task TryCloseBinanceOrder(CryptoCurrency currency, long order)
    {
        if (!_exchangeServiceConfig.Status)
        {
            return;
        }

        // Log that an order is being canceled
        _logger.LogInformation($"Canceling order '{order}' ({currency.ToString()})");

        // Get information about the order from the Binance API
        var orderData = await _binanceClient.UsdFuturesApi.Trading.GetOrderAsync(currency.ToString(), order);

        // If the API call was successful, attempt to cancel the order
        if (orderData.Success)
        {
            var cancelOrderData =
                await _binanceClient.UsdFuturesApi.Trading.CancelOrderAsync(currency.ToString(), order);

            // If the cancellation was unsuccessful, log an error message and return
            if (!cancelOrderData.Success)
            {
                _logger.LogError($"Can not cancel order {order} ({currency.ToString()})");
                return;
            }

            // If the cancellation was successful, log a message indicating that the order was canceled
            _logger.LogInformation($"Canceled order '{order}'");
        }
        // If the API call to get order information was unsuccessful, log a warning message
        else
        {
            _logger.LogWarning(
                $"Tried to close order '{order}' with {currency.ToString().ToUpper()}, but can not find this order");
        }
    }

    // This method is called when a buy or sell order is finished executing.
    // It takes a BinanceCurrency object representing the currency of the order and a boolean indicating whether it was a buy or sell order.
    public void NotifyFinished(CryptoCurrency currency, bool buyPosition)
    {
        // If the currency, buy/sell position, and position status match the current state of the object, update the state and log a message
        if (currency == CurrentCurrency && buyPosition == Buy && HasPosition)
        {
            string buyString = Buy ? "Buy" : "Sell";
            _logger.LogInformation($"Order finished: {CurrentCurrency.ToString()} ({buyString})");

            HasPosition = false;
            CurrentCurrency = CryptoCurrency.None;
            Enter = 0.0m;
            OrderId = "";
            OrderStarted = DateTime.MinValue;
        }
    }

    public async Task RequestBuy(CryptoCurrency currency, decimal takeProfit, decimal stopLoss)
    {
        var listenKey = await _binanceClient.UsdFuturesApi.Account.StartUserStreamAsync();
        _logger.LogInformation($"Requested BUY ({currency.ToString()})");
        try
        {
            await RequestOrder(true, currency, takeProfit, stopLoss);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public async Task RequestSell(CryptoCurrency currency, decimal takeProfit, decimal stopLoss)
    {
        _logger.LogInformation($"Requested SELL ({currency.ToString()})");
        try
        {
            await RequestOrder(false, currency, takeProfit, stopLoss);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    private async Task RequestBinanceOrder(Binance.Net.Enums.OrderSide side, CryptoCurrency currency, decimal cost,
        decimal takeProfit, decimal stopLoss)
    {
        _logger.LogWarning($"Interacting with finances, as the status is ON)");
        await _binanceClient.UsdFuturesApi.Account.ChangeMarginTypeAsync(currency.ToString(),
            Binance.Net.Enums.FuturesMarginType.Isolated);
        await _binanceClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(currency.ToString(),
            leverage: _exchangeServiceConfig.Leverage);
        _logger.LogInformation(
            $"'{currency.ToString()}' Set FuturesMarginType to Isolated, Leverage to {_exchangeServiceConfig.Leverage}x");

        // Retrieve bot's current balance and calculate trade amount
        decimal balance = await GetUsdtFuturesBalance();
        decimal balancePercent = balance * _exchangeServiceConfig.OrderSizePercent;
        decimal effectiveBalance = balancePercent * _exchangeServiceConfig.Leverage;
        decimal amount = Math.Round(effectiveBalance / cost, 2);

        // Log trade details
        _logger.LogInformation(
            $"Trade: {side.ToString()} {amount} {currency.ToString()}: {cost}, TP: {takeProfit}, SL: {stopLoss}");

        // Round take profit and stop loss prices to 2 decimal places
        takeProfit = Math.Round(takeProfit, 2);
        stopLoss = Math.Round(stopLoss, 2);

        _logger.LogWarning($"Placing trade on binance...");
        // Place market order using Binance API
        WebCallResult<Binance.Net.Objects.Models.Futures.BinanceFuturesPlacedOrder> openPositionResult =
            await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(currency.ToString().ToUpper(),
                side, Binance.Net.Enums.FuturesOrderType.Market, amount);
        OrderId = openPositionResult.Data.Id.ToString();

        // Throw exception if order placement was unsuccessful
        if (!openPositionResult.Success)
            throw new Exception(openPositionResult.Error?.Message);

        _logger.LogWarning($"Set SL on binance..");
        // Place stop loss order using Binance API
        WebCallResult<Binance.Net.Objects.Models.Futures.BinanceFuturesPlacedOrder> stopLossResult
            = await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(currency.ToString().ToUpper(),
                side == Binance.Net.Enums.OrderSide.Buy
                    ? Binance.Net.Enums.OrderSide.Sell
                    : Binance.Net.Enums.OrderSide.Buy, Binance.Net.Enums.FuturesOrderType.StopMarket,
                quantity: null, closePosition: true, stopPrice: stopLoss);

        // If stop loss placement was unsuccessful, try to close the original order and throw exception
        if (!stopLossResult.Success)
        {
            await TryCloseBinanceOrder(currency, long.Parse(OrderId));
            throw new Exception(stopLossResult.Error?.Message);
        }

        _logger.LogWarning($"Set TP on binance..");
        // Place take profit order using Binance API
        WebCallResult<Binance.Net.Objects.Models.Futures.BinanceFuturesPlacedOrder> takeProfitResult
            = await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(currency.ToString().ToUpper(),
                side == Binance.Net.Enums.OrderSide.Buy
                    ? Binance.Net.Enums.OrderSide.Sell
                    : Binance.Net.Enums.OrderSide.Buy,
                Binance.Net.Enums.FuturesOrderType.TakeProfitMarket,
                quantity: null, closePosition: true, stopPrice: takeProfit);

        // If take profit placement was unsuccessful, try to close the original order and throw exception
        if (!takeProfitResult.Success)
        {
            await TryCloseBinanceOrder(currency, long.Parse(OrderId));
            throw new Exception(takeProfitResult.Error?.Message);
        }
    }

    private async Task RequestByBitOrder(Bybit.Net.Enums.OrderSide side, CryptoCurrency currency, decimal cost,
        decimal takeProfit, decimal stopLoss)
    {
        _logger.LogWarning($"Interacting with finances, as the status is ON)");
        await _bybitClient.UsdPerpetualApi.Account.SetLeverageAsync(currency.ToString(),
            buyLeverage: _exchangeServiceConfig.Leverage, sellLeverage: _exchangeServiceConfig.Leverage);
        _logger.LogInformation($"'{currency.ToString()}' Set Leverage to {_exchangeServiceConfig.Leverage}x");

        // Retrieve bot's current balance and calculate trade amount
        decimal balance = await GetUsdtFuturesBalance();
        decimal balancePercent = balance * _exchangeServiceConfig.OrderSizePercent;
        decimal effectiveBalance = balancePercent * _exchangeServiceConfig.Leverage;
        decimal amount = Math.Round(effectiveBalance / cost, 2);

        // Log trade details
        _logger.LogInformation(
            $"Trade: {side.ToString()} {amount} {currency.ToString()}: {cost}, TP: {takeProfit}, SL: {stopLoss}");

        // Round take profit and stop loss prices to 2 decimal places
        takeProfit = Math.Round(takeProfit, 2);
        stopLoss = Math.Round(stopLoss, 2);

        _logger.LogWarning($"Placing trade on bybit...");
        // Place market order using Binance API
        WebCallResult<Bybit.Net.Objects.Models.BybitUsdPerpetualOrder> openPositionRes =
            await _bybitClient.UsdPerpetualApi.Trading.PlaceOrderAsync(currency.ToString().ToUpper(),
                side,
                Bybit.Net.Enums.OrderType.Market,
                amount,
                Bybit.Net.Enums.TimeInForce.GoodTillCanceled, reduceOnly: false, closeOnTrigger: true,
                takeProfitPrice: takeProfit,
                stopLossPrice: stopLoss);

        OrderId = openPositionRes.Data.Id;

        // Throw exception if order placement was unsuccessful
        if (!openPositionRes.Success)
            throw new Exception(openPositionRes.Error.Message);
    }

    public async Task RequestOrder(bool longSide, CryptoCurrency currency, decimal takeProfit, decimal stopLoss)
    {
        // Check if bot already has a position open
        if (HasPosition)
        {
            string buyString = Buy ? "Buy" : "Sell";
            throw new Exception($"Bot already has position: {CurrentCurrency} {buyString}");
        }

        decimal cost = await GetAvgPrice(currency);
        Enter = cost;

        if (_exchangeServiceConfig.Status)
        {
            if (_botConfig.UseBinance)
            {
                await RequestBinanceOrder(longSide ? Binance.Net.Enums.OrderSide.Buy : Binance.Net.Enums.OrderSide.Sell,
                    currency, cost, takeProfit, stopLoss);
            }
            else
            {
                await RequestByBitOrder(longSide ? Bybit.Net.Enums.OrderSide.Buy : Bybit.Net.Enums.OrderSide.Sell, 
                    currency, cost, takeProfit, stopLoss);
            }
        }
        else
        {
            _logger.LogWarning($"Do not interact with finances, as the status is OFF");
        }

        // Set bot's position to open and update trade details
        CurrentCurrency = currency;
        Buy = longSide;
        HasPosition = true;
        OrderStarted = DateTime.Now;
    }


    public async Task ClosePosition()
    {
        _logger.LogInformation("Closing order at market price");
        if (_exchangeServiceConfig.Status)
        {
            if (_botConfig.UseBinance)
            {
                await _binanceClient.UsdFuturesApi.Trading.CancelAllOrdersAsync(CurrentCurrency.ToString());
            }
            else
            {
                await _bybitClient.UsdPerpetualApi.Trading.CancelAllOrdersAsync(CurrentCurrency.ToString());
;            }
        }
        else
        {
            _logger.LogWarning($"Do not interact with finances, as the status is OFF");
        }
    }
}