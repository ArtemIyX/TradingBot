
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects;
using Bybit.Net.Objects.Models;
using Bybit.Net.Objects.Models.Socket;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Configuration;
using System.Globalization;
using TradingBot.Data;
using TradingBot.Data.Config;
using TradingBot.Extensions;

namespace TradingBot.Services;

internal class BrokerService
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookService> _logger;
    private readonly ExchangeServiceConfig _exchangeServiceConfig;
    private readonly BotConfig _botConfig;
    private readonly BybitClient _bybitClient;
    private readonly BybitSocketClient _bybitSocketClient;

    public CryptoCurrency CurrentCurrency { get; private set; }
    public bool HasPosition { get; private set; }
    public bool Buy { get; private set; }
    public decimal Enter { get; private set; }
    public string OrderId { get; private set; }
    public decimal Qty { get; private set; }
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
        _botConfig = _config.GetSection("Bot").Get<BotConfig>() ??
                     throw new InvalidOperationException("Can not get 'Bot' from settings");

        var credits = new ApiCredentials(_exchangeServiceConfig.ApiKey, _exchangeServiceConfig.SecretKey);

        _bybitClient = new BybitClient(new BybitClientOptions()
        {
            ApiCredentials = credits
        });

        _bybitSocketClient = new BybitSocketClient(new BybitSocketClientOptions()
        {
            ApiCredentials = credits
        });

    }

    public async Task ConnectToStream()
    {

        await _bybitSocketClient.UsdPerpetualStreams.SubscribeToOrderUpdatesAsync(OnOrderUpdate);
        _logger.LogInformation("Connected to ByBit stream api");
    }

    private void TryNotify(OrderSide side, bool tp, decimal price)
    {
        if (!HasPosition)
        {
            _logger.LogWarning("Bot dont have position to close");
            return;
        }
        bool sellPos = side == OrderSide.Sell && !Buy;
        bool buyPos = side == OrderSide.Buy && Buy;
        bool correctPos = sellPos || buyPos;
        if (!correctPos)
        {
            string sidestr = Buy ? "Buy" : "Sell";
            _logger.LogWarning($"Uncorrect position ({side}, when we have {sidestr})");
            return;
        }
        TpSlReached?.Invoke(new MonitorResult()
        {
            Price = price,
            Buy = this.Buy,
            Take = tp,
            Currency = CurrentCurrency
        });

    }

    private void OnOrderUpdate(DataEvent<IEnumerable<BybitUsdPerpetualOrderUpdate>> dataUpdate)
    {
        BybitUsdPerpetualOrderUpdate? update = dataUpdate.Data.FirstOrDefault();
        if (update == null)
            return;

        string updateType = update.CreateType;
        string info = $"{update.Symbol} {update.Side}";

        CryptoCurrency cur = ServiceExtensions.ToCrtypoCurrency(update.Symbol);
        OrderSide invertSide = update.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
        switch (updateType)
        {
            
            case "CreateByTakeProfit":
                _logger.LogInformation($"Stream: {info} was closed by take profit");
                TryNotify(invertSide, true, update.LastTradePrice);
                break;
            case "CreateByStopLoss":
                _logger.LogInformation($"Stream: {info} was closed by stop loss");
                TryNotify(invertSide, false, update.LastTradePrice);
                break;
            case "CreateByClosing":
                _logger.LogInformation($"Stream: {info} was closed");
                if(cur == CurrentCurrency)
                {
                    NotifyFinished(cur, update.Side == OrderSide.Buy);
                }
                break;
            default:
                _logger.LogInformation($"Stream: {info} was {update.CreateType}");
                break;
        }
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
            //Console.Clear();
            Console.WriteLine($"Monitoring {currency}: {currentPrice} (TP: {take}, SP: {stop})");
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

            await Task.Delay(50, cancellationToken);
        }
    }

    // This method retrieves the available balance of USDT in a Binance futures account.
    public async Task<decimal> GetUsdtFuturesBalance()
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

    // This method asynchronously retrieves the current average price for the specified currency from the Binance API.
    // It takes a BinanceCurrency object as a parameter and returns a decimal value representing the average price.
    public async Task<decimal> GetAvgPrice(CryptoCurrency currency)
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
        string lastPrice = (result["last_price"] ?? throw new InvalidOperationException()).Value<string>() ??
                           throw new InvalidOperationException();
        CultureInfo culture = CultureInfo.InvariantCulture;
        decimal price = decimal.Parse(lastPrice, NumberStyles.Number, culture);
        return price;
    }

    public async Task<TakeProfitStopLossResult> CalculateTPSL_Advanced(CryptoCurrency currency, bool buy, decimal take,
        decimal loss,
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
            Qty = 0.0m;
            Buy = false;
        }
    }

    public async Task RequestBuy(CryptoCurrency currency, decimal takeProfit, decimal stopLoss)
    {
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

    public async Task<IEnumerable<BybitKline>> GetCandles(CryptoCurrency currency, KlineInterval interval, int limit, DateTime From, DateTime To)
    {
        var res = await _bybitClient.DerivativesApi.ExchangeData.GetKlinesAsync(Category.Linear, currency.ToString(), interval, From, To, limit);
        if(!res.Success)
        {
            throw new Exception("Can not get klines: " + res.Error?.Message);
        }
        return res.Data;
    }

    private async Task RequestByBitOrder(Bybit.Net.Enums.OrderSide side, CryptoCurrency currency, decimal cost,
        decimal? takeProfit, decimal? stopLoss)
    {
        await ServiceExtensions.SyncTime(_logger);

        _logger.LogWarning($"Interacting with finances, as the status is ON");
        await _bybitClient.UsdPerpetualApi.Account.SetLeverageAsync(currency.ToString(),
            buyLeverage: _exchangeServiceConfig.Leverage, sellLeverage: _exchangeServiceConfig.Leverage);
        _logger.LogInformation($"'{currency.ToString()}' Set Leverage to {_exchangeServiceConfig.Leverage}x");

        // Retrieve bot's current balance and calculate trade amount
        decimal balance = await GetUsdtFuturesBalance();
        decimal percent = _exchangeServiceConfig.OrderSizePercent;
        decimal leverage = _exchangeServiceConfig.Leverage;
        decimal qty = Math.Round((((balance * percent) * leverage) / cost), 4);

        string? tpInfo = takeProfit == null ? "NA" : takeProfit.ToString();
        string? slInfo = stopLoss == null ? "NA" : stopLoss.ToString();
        // Log trade details
        _logger.LogInformation(
            $"Trade: {side.ToString()} {qty} {currency.ToString()}: {cost}, TP: {tpInfo}, SL: {stopLoss}");

        // Round take profit and stop loss prices to 2 decimal places
        if (takeProfit != null)
            takeProfit = Math.Round((decimal)takeProfit, 4);
        if(stopLoss != null)
            stopLoss = Math.Round((decimal)stopLoss, 4);

        _logger.LogWarning($"Placing trade on bybit...");
        // Place market order using Binance API
        WebCallResult<Bybit.Net.Objects.Models.BybitUsdPerpetualOrder> openPositionRes =
            await _bybitClient.UsdPerpetualApi.Trading.PlaceOrderAsync(
                symbol: currency.ToString().ToUpper(),
                side: side,
                type: Bybit.Net.Enums.OrderType.Market,
                quantity: qty,
                timeInForce: Bybit.Net.Enums.TimeInForce.GoodTillCanceled,
                reduceOnly: false,
                closeOnTrigger: false,
                positionMode: PositionMode.OneWay,
                takeProfitPrice: takeProfit,
                stopLossPrice: stopLoss);


        // Throw exception if order placement was unsuccessful
        if (!openPositionRes.Success)
            throw new Exception("Open position error:" + openPositionRes?.Error?.Message);

        if (openPositionRes.Data == null)
            throw new Exception("OpenPosition.Data is null");

        OrderId = openPositionRes.Data.Id;
        Qty = qty;
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
            await RequestByBitOrder(
                longSide ? Bybit.Net.Enums.OrderSide.Buy : Bybit.Net.Enums.OrderSide.Sell,
                currency,
                cost,
                takeProfit,
                stopLoss);
        }
        else
        {
            _logger.LogWarning($"RequestOrder: Do not interact with finances, as the status is OFF");
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
            WebCallResult<BybitUsdPerpetualOrder> closeRes = await _bybitClient.UsdPerpetualApi.Trading.PlaceOrderAsync(
                symbol: CurrentCurrency.ToString(),
                side: Buy ? OrderSide.Sell : OrderSide.Buy,
                type: OrderType.Market,
                quantity: Qty,
                timeInForce: TimeInForce.GoodTillCanceled,
                reduceOnly: true,
                closeOnTrigger: false,
                positionMode: PositionMode.OneWay);

            if (!closeRes.Success)
            {
                throw new Exception("Close result error:" + closeRes.Error.Message);
            }
        }
        else
        {
            _logger.LogWarning($"ClosePosition: Do not interact with finances, as the status is OFF");
        }
    }
}