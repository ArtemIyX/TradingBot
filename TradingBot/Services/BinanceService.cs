using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TradingBot.Data;

namespace TradingBot.Services;

public enum BinanceCurrency
{
    BTCUSDT,
    LTCUSDT,
    ETHUSDT,
    NEOUSDT,
    BNBUSDT,
    BNXUSDT,
    QTUMUSDT,
    EOSUSDT,
    SNTUSDT,
    BNTUSDT,
    GASUSDT,
    BCCUSDT,
    USDTUSDT,
    HSRUSDT,
    OAXUSDT,
    DNTUSDT,
    MCOUSDT,
    ICNUSDT,
    ZRXUSDT,
    OMGUSDT,
    WTCUSDT,
    YOYOUSDT,
    LRCUSDT,
    TRXUSDT,
    SNGLSUSDT,
    STRATUSDT,
    BQXUSDT,
    FUNUSDT,
    KNCUSDT,
    CDTUSDT,
    XVGUSDT,
    IOTAUSDT,
    SNMUSDT,
    LINKUSDT,
    CVCUSDT,
    TNTUSDT,
    REPUSDT,
    MDAUSDT,
    MTLUSDT,
    SALTUSDT,
    NULSUSDT,
    SUBUSDT,
    STXUSDT,
    MTHUSDT,
    ADXUSDT,
    ETCUSDT,
    ENGUSDT,
    ZECUSDT,
    ASTUSDT,
    GNTUSDT,
    DGDUSDT,
    BATUSDT,
    DASHUSDT,
    POWRUSDT,
    BTGUSDT,
    REQUSDT,
    XMRUSDT,
    EVXUSDT,
    VIBUSDT,
    ENJUSDT,
    VENUSDT,
    ARKUSDT,
    XRPUSDT,
    MODUSDT,
    STORJUSDT,
    KMDUSDT,
    RCNUSDT,
    EDOUSDT,
    DATAUSDT,
    DLTUSDT,
    MANAUSDT,
    PPTUSDT,
    RDNUSDT,
    GXSUSDT,
    AMBUSDT,
    ARNUSDT,
    BCPTUSDT,
    CNDUSDT,
    GVTUSDT,
    POEUSDT,
    BTSUSDT,
    FUELUSDT,
    XZCUSDT,
    QSPUSDT,
    LSKUSDT,
    BCDUSDT,
    TNBUSDT,
    ADAUSDT,
    LENDUSDT,
    XLMUSDT,
    CMTUSDT,
    WAVESUSDT,
    WABIUSDT,
    GTOUSDT,
    ICXUSDT,
    OSTUSDT,
    ELFUSDT,
    AIONUSDT,
    WINGSUSDT,
    BRDUSDT,
    NEBLUSDT,
    NAVUSDT,
    VIBEUSDT,
    LUNUSDT,
    TRIGUSDT,
    APPCUSDT,
    CHATUSDT,
    RLCUSDT,
    INSUSDT,
    PIVXUSDT,
    IOSTUSDT,
    STEEMUSDT,
    NANOUSDT,
    AEUSDT,
    VIAUSDT,
    BLZUSDT,
    SYSUSDT,
    RPXUSDT,
    NCASHUSDT,
    POAUSDT,
    ONTUSDT,
    ZILUSDT,
    STORMUSDT,
    XEMUSDT,
    WANUSDT,
    WPRUSDT,
    QLCUSDT,
    GRSUSDT,
    CLOAKUSDT,
    LOOMUSDT,
    BCNUSDT,
    TUSDUSDT,
    ZENUSDT,
    SKYUSDT,
    THETAUSDT,
    IOTXUSDT,
    QKCUSDT,
    AGIUSDT,
    NXSUSDT,
    SCUSDT,
    NPXSUSDT,
    KEYUSDT,
    NASUSDT,
    MFTUSDT,
    DENTUSDT,
    IQUSDT,
    ARDRUSDT,
    HOTUSDT,
    VETUSDT,
    DOCKUSDT,
    POLYUSDT,
    VTHOUSDT,
    ONGUSDT,
    PHXUSDT,
    HCUSDT,
    GOUSDT,
    PAXUSDT,
    RVNUSDT,
    DCRUSDT,
    USDCUSDT,
    MITHUSDT,
    BCHABCUSDT,
    BCHSVUSDT,
    RENUSDT,
    BTTUSDT,
    USDSUSDT,
    FETUSDT,
    TFUELUSDT,
    CELRUSDT,
    MATICUSDT,
    ATOMUSDT,
    PHBUSDT,
    ONEUSDT,
    FTMUSDT,
    BTCBUSDT,
    USDSBUSDT,
    CHZUSDT,
    COSUSDT,
    ALGOUSDT,
    ERDUSDT,
    DOGEUSDT,
    BGBPUSDT,
    DUSKUSDT,
    ANKRUSDT,
    WINUSDT,
    TUSDBUSDT,
    COCOSUSDT,
    PERLUSDT,
    TOMOUSDT,
    BUSDUSDT,
    BANDUSDT,
    BEAMUSDT,
    HBARUSDT,
    XTZUSDT,
    NGNUSDT,
    DGBUSDT,
    NKNUSDT,
    GBPUSDT,
    EURUSDT,
    KAVAUSDT,
    RUBUSDT,
    UAHUSDT,
    ARPAUSDT,
    TRYUSDT,
    CTXCUSDT,
    AERGOUSDT,
    BCHUSDT,
    TROYUSDT,
    BRLUSDT,
    VITEUSDT,
    FTTUSDT,
    AUDUSDT,
    OGNUSDT,
    DREPUSDT,
    BULLUSDT,
    BEARUSDT,
    ETHBULLUSDT,
    ETHBEARUSDT,
    XRPBULLUSDT,
    XRPBEARUSDT,
    EOSBULLUSDT,
    EOSBEARUSDT,
    TCTUSDT,
    WRXUSDT,
    LTOUSDT,
    ZARUSDT,
    MBLUSDT,
    COTIUSDT,
    BKRWUSDT,
    BNBBULLUSDT,
    BNBBEARUSDT,
    HIVEUSDT,
    STPTUSDT,
    SOLUSDT,
    IDRTUSDT,
    CTSIUSDT,
    CHRUSDT,
    BTCUPUSDT,
    BTCDOWNUSDT,
    HNTUSDT,
    JSTUSDT,
    FIOUSDT,
    BIDRUSDT,
    STMXUSDT,
    MDTUSDT,
    PNTUSDT,
    COMPUSDT,
    IRISUSDT,
    MKRUSDT,
    SXPUSDT,
    SNXUSDT,
    DAIUSDT,
    ETHUPUSDT,
    ETHDOWNUSDT,
    ADAUPUSDT,
    ADADOWNUSDT,
    LINKUPUSDT,
    LINKDOWNUSDT,
    DOTUSDT,
    RUNEUSDT,
    BNBUPUSDT,
    BNBDOWNUSDT,
    XTZUPUSDT,
    XTZDOWNUSDT,
    AVAUSDT,
    BALUSDT,
    YFIUSDT,
    SRMUSDT,
    ANTUSDT,
    CRVUSDT,
    SANDUSDT,
    OCEANUSDT,
    NMRUSDT,
    LUNAUSDT,
    IDEXUSDT,
    RSRUSDT,
    PAXGUSDT,
    WNXMUSDT,
    TRBUSDT,
    EGLDUSDT,
    BZRXUSDT,
    WBTCUSDT,
    KSMUSDT,
    SUSHIUSDT,
    YFIIUSDT,
    DIAUSDT,
    BELUSDT,
    UMAUSDT,
    EOSUPUSDT,
    TRXUPUSDT,
    EOSDOWNUSDT,
    TRXDOWNUSDT,
    XRPUPUSDT,
    XRPDOWNUSDT,
    DOTUPUSDT,
    DOTDOWNUSDT,
    NBSUSDT,
    WINGUSDT,
    SWRVUSDT,
    LTCUPUSDT,
    LTCDOWNUSDT,
    CREAMUSDT,
    UNIUSDT,
    OXTUSDT,
    SUNUSDT,
    AVAXUSDT,
    BURGERUSDT,
    BAKEUSDT,
    FLMUSDT,
    FLOWUSDT,
    SCRTUSDT,
    XVSUSDT,
    CAKEUSDT,
    SPARTAUSDT,
    UNIUPUSDT,
    UNIDOWNUSDT,
    ALPHAUSDT,
    ORNUSDT,
    UTKUSDT,
    NEARUSDT,
    VIDTUSDT,
    AAVEUSDT,
    FILUSDT,
    SXPUPUSDT,
    SXPDOWNUSDT,
    INJUSDT,
    FILDOWNUSDT,
    FILUPUSDT,
    YFIUPUSDT,
    YFIDOWNUSDT,
    CTKUSDT,
    EASYUSDT,
    AUDIOUSDT,
    BCHUPUSDT,
    BCHDOWNUSDT,
    BOTUSDT,
    AXSUSDT,
    AKROUSDT,
    HARDUSDT,
    KP3RUSDT,
    RENBTCUSDT,
    SLPUSDT,
    STRAXUSDT,
    UNFIUSDT,
    CVPUSDT,
    BCHAUSDT,
    FORUSDT,
    FRONTUSDT,
    ROSEUSDT,
    HEGICUSDT,
    AAVEUPUSDT,
    AAVEDOWNUSDT,
    PROMUSDT,
    BETHUSDT,
    SKLUSDT,
    GLMUSDT,
    SUSDUSDT,
    COVERUSDT,
    GHSTUSDT,
    SUSHIUPUSDT,
    SUSHIDOWNUSDT,
    XLMUPUSDT,
    XLMDOWNUSDT,
    DFUSDT,
    JUVUSDT,
    PSGUSDT,
    BVNDUSDT,
    GRTUSDT,
    CELOUSDT,
    TWTUSDT,
    REEFUSDT,
    OGUSDT,
    ATMUSDT,
    ASRUSDT,
    INCHUSDT,
    RIFUSDT,
    BTCSTUSDT,
    TRUUSDT,
    DEXEUSDT,
    CKBUSDT,
    FIROUSDT,
    LITUSDT,
    PROSUSDT,
    VAIUSDT,
    SFPUSDT,
    FXSUSDT,
    DODOUSDT,
    AUCTIONUSDT,
    UFTUSDT,
    ACMUSDT,
    PHAUSDT,
    TVKUSDT,
    BADGERUSDT,
    FISUSDT,
    OMUSDT,
    PONDUSDT,
    ALICEUSDT,
    DEGOUSDT,
    BIFIUSDT,
    LINAUSDT,
    OPUSDT,
    LQTYUSDT,
    MASKUSDT,
    CFXUSDT,
    None
}

public static class BinanceServiceExtensions
{
    public static BinanceCurrency ToBinanceCurrency(this string input)
    {
        BinanceCurrency currency;
        if (Enum.TryParse(input, out currency))
        {
            return currency;
        }
        else
        {
            return BinanceCurrency.None;
        }
    }

    public static string ConvertKlineIntervalToString(KlineInterval interval)
    {
        switch (interval)
        {
            case KlineInterval.OneSecond:
                return "1s";
            case KlineInterval.OneMinute:
                return "1m";
            case KlineInterval.ThreeMinutes:
                return "3m";
            case KlineInterval.FiveMinutes:
                return "5m";
            case KlineInterval.FifteenMinutes:
                return "15m";
            case KlineInterval.ThirtyMinutes:
                return "30m";
            case KlineInterval.OneHour:
                return "1h";
            case KlineInterval.TwoHour:
                return "2h";
            case KlineInterval.FourHour:
                return "4h";
            case KlineInterval.SixHour:
                return "6h";
            case KlineInterval.EightHour:
                return "8h";
            case KlineInterval.TwelveHour:
                return "12h";
            case KlineInterval.OneDay:
                return "1d";
            case KlineInterval.ThreeDay:
                return "3d";
            case KlineInterval.OneWeek:
                return "1w";
            case KlineInterval.OneMonth:
                return "1M";
            default:
                throw new ArgumentOutOfRangeException(nameof(interval), interval, null);
        }
    }
}
public struct MonitorResult
{
    public BinanceCurrency Currency { get; set; }
    public bool Take { get; set; }
    public bool Buy { get; set; }
    public decimal Price { get; set; }
}

public struct TPSLResult
{
    public decimal Price { get; set; } 
    public decimal Take { get; set; } 
    public decimal Loss { get; set; }
    public bool Succes { get; set; }
}

public struct BinanceCandlestickData
{
    public long OpenTime { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public long CloseTime { get; set; }
}

public delegate Task CurrencyTPSLDelegate(MonitorResult Result);

internal class BinanceService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookService> _logger;
    private readonly BinanceConfig _binanceConfig;
    private BinanceClient _binanceClient;

    public BinanceCurrency CurrentCurrency { get; private set; }
    public bool HasPosition { get; private set; }
    public bool Buy { get; private set; }
    public decimal Enter { get; private set; }
    public long OrderId { get; private set; }
    public DateTime OrderStarted { get; set; }

    public event CurrencyTPSLDelegate TPSLReached;

    public BinanceService(IConfiguration Config,
                     ILogger<WebhookService> Logger)
    {
        CurrentCurrency = BinanceCurrency.None;
        HasPosition = false;
        _config = Config;
        _logger = Logger;

        _binanceConfig = _config.GetSection("Binance").Get<BinanceConfig>();

        BinanceClientOptions clientOption = new BinanceClientOptions
        {
            ApiCredentials = new BinanceApiCredentials(_binanceConfig.ApiKey, _binanceConfig.SecretKey)
        };

        if (_binanceConfig.TestNet)
        {
            clientOption.UsdFuturesApiOptions = new BinanceApiClientOptions()
            {
                BaseAddress = "https://testnet.binancefuture.com"
            };
        }
        _binanceClient = new BinanceClient(clientOption);
        
       
    }
    public async Task StartMonitorTPSL(BinanceCurrency currency, bool buy, decimal take, decimal stop)
    {
        _logger.LogInformation("Started TPSL monitor...");
        MonitorResult result = new MonitorResult();
        result.Buy = buy;
        result.Currency = currency;
        int i = 1;
        int need = 5;
        while(true)
        {
            decimal current = await GetAvgPrice(currency);
            i++;
            _logger.LogInformation($"Monitoring {currency}: {current} (TP: {take}, SP: {stop})");
            if (i == need)
            {
                i = 1;
            }
            if (buy)
            {
                if(current >= take)
                {
                    result.Take = true;
                    result.Price = current;
                    TPSLReached?.Invoke(result);
                    return;
                }
                else if(current <= stop)
                {
                    result.Take = false;
                    result.Price = current;
                    TPSLReached?.Invoke(result);
                    return;
                }
            }
            else
            {
                if(current <= take)
                {
                    result.Take = true;
                    result.Price = current;
                    TPSLReached?.Invoke(result);
                    return;
                }
                else if(current >= stop)
                {
                    result.Take = false;
                    result.Price = current;
                    TPSLReached?.Invoke(result);
                    return;
                }
            }
            await Task.Delay(1000);
            
        }
    }

    // This method retrieves the available balance of USDT in a Binance futures account.
    public async Task<decimal> GetUsdtFuturesBalance()
    {
        // Use the Binance .NET library to call the USDT Futures API and get account balances.
        WebCallResult<IEnumerable<Binance.Net.Objects.Models.Futures.BinanceFuturesAccountBalance>> res
            = await _binanceClient.UsdFuturesApi.Account.GetBalancesAsync();

        // If the API call was unsuccessful, throw an exception with the error message.
        if (!res.Success)
            throw new Exception(res.Error.Message);

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

    // This method asynchronously retrieves the current average price for the specified currency from the Binance API.
    // It takes a BinanceCurrency object as a parameter and returns a decimal value representing the average price.
    public async Task<decimal> GetAvgPrice(BinanceCurrency currency)
    {
        // Call the Binance API to get the current average price for the currency
        //var res = await _binanceClient.UsdFuturesApi.ExchangeData.GetPriceAsync(currency.ToString().ToUpper());
        //var res = await _binanceClient.SpotApi.ExchangeData.GetCurrentAvgPriceAsync(currency.ToString().ToUpper());
        //var res = await _binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(currency.ToString(),
        //            (KlineInterval)_binanceConfig.KlinePeriod, limit: 1);
        var client = new RestClient("https://fapi.binance.com");
        var request = new RestRequest("/fapi/v1/ticker/price", Method.Get);
        request.AddParameter("symbol", currency.ToString().ToUpper());

        RestResponse response = await client.ExecuteAsync(request);
        dynamic data = JsonConvert.DeserializeObject<dynamic>(response.Content ?? throw new Exception("Can not get Content from price request")) 
            ?? throw new Exception("Can not parse json from price response");
        return data.price;
    }

    

    public async Task<List<BinanceCandlestickData>> GetRecentCandles(BinanceCurrency currency, KlineInterval interval, int limit)
    {
        string symbol = currency.ToString().ToUpper();
        string inter = BinanceServiceExtensions.ConvertKlineIntervalToString(interval);
        
        string apiUrl = $"https://api.binance.com/api/v3/klines?symbol={symbol}&interval={inter}&limit={limit}";
        List<BinanceCandlestickData> res = new List<BinanceCandlestickData>();
        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<object[][]>(json);
                CultureInfo culture = CultureInfo.InvariantCulture;
                foreach (var candle in data)
                {
                   res.Add(new BinanceCandlestickData{
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
        }
        return res;
    }

    public async Task<TPSLResult> CalculateTPSL(BinanceCurrency currency, bool buy, decimal TakePercent)
    {
        // Determine where you've entered and in what direction
        /* longStop = strategy.position_avg_price * (1 - stopPer)
         shortStop = strategy.position_avg_price * (1 + stopPer)
         shortTake = strategy.position_avg_price * (1 - takePer)
         longTake = strategy.position_avg_price * (1 + takePer)*/
        try
        {
            decimal avgPprice = await GetAvgPrice(currency);

            var candles = await GetRecentCandles(currency, (KlineInterval)_binanceConfig.KlinePeriod, _binanceConfig.SwingLen);

            decimal sw_high = candles.Max(x => x.High);
            decimal sw_low = candles.Min(x => x.Low);

            decimal buyTakeProfit = avgPprice + Math.Abs(avgPprice - sw_low) * TakePercent;
            decimal sellTakeProfit = avgPprice - Math.Abs(sw_high - avgPprice) * TakePercent;

            decimal actualProfit = buy ? buyTakeProfit : sellTakeProfit;
            decimal actualLoss = buy ? sw_low : sw_high;

            string label = buy ? "BUY" : "SELL";
            _logger.LogInformation($"Calculating TPSL for {label}. Price: {avgPprice}, TP: {actualProfit}, SL: {actualLoss}");

            if (buy)
            {
                if (sw_low > avgPprice)
                {
                    throw new Exception($"Calculating TPSL for {label} {currency} at {avgPprice}, but recent swing low is {sw_low}");
                }
            }
            else
            {
                if(sw_high < avgPprice)
                {
                    throw new Exception($"Calculating TPSL for {label} {currency} at {avgPprice}, but recent swing high is {sw_high}");
                }  
            }
            return new TPSLResult()
            {
                Price = avgPprice,
                Take = actualProfit,
                Loss = actualLoss,
                Succes = true
            };

        }
        catch(Exception ex)
        {
            _logger.LogError(ex.Message);
            return new TPSLResult()
            {
                Succes = false
            };
        }
    }

    // This method asynchronously attempts to cancel an open order for the specified currency and order ID.
    // It takes a BinanceCurrency object and a long integer representing the order ID as parameters.
    private async Task TryCloseOrder(BinanceCurrency currency, long order)
    {
        if (!_binanceConfig.Status)
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
            var cancelOrderData = await _binanceClient.UsdFuturesApi.Trading.CancelOrderAsync(currency.ToString(), order);

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
            _logger.LogWarning($"Tried to close order '{order}' with {currency.ToString().ToUpper()}, but can not find this order");
        }
    }

    // This method is called when a buy or sell order is finished executing.
    // It takes a BinanceCurrency object representing the currency of the order and a boolean indicating whether it was a buy or sell order.
    public void NotifyFinished(BinanceCurrency currency, bool buyPosition)
    {
        // If the currency, buy/sell position, and position status match the current state of the object, update the state and log a message
        if (currency == CurrentCurrency && buyPosition == Buy && HasPosition)
        {
            string buyString = Buy ? "Buy" : "Sell";
            _logger.LogInformation($"Order finished: {CurrentCurrency.ToString()} ({buyString})");

            HasPosition = false;
            CurrentCurrency = BinanceCurrency.None;
            Enter = 0.0m;
            OrderId = 0;
            OrderStarted = DateTime.MinValue;
        }
    }

    public async Task RequestBuy(BinanceCurrency currency, decimal takeProfit, decimal stopLoss)
    {
        var listenKey = await _binanceClient.UsdFuturesApi.Account.StartUserStreamAsync();
        _logger.LogInformation($"Requested BUY ({currency.ToString()})");
        try
        {
            await RequestOrder(OrderSide.Buy, currency, takeProfit, stopLoss);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public async Task RequestSell(BinanceCurrency currency, decimal takeProfit, decimal stopLoss)
    {
        _logger.LogInformation($"Requested SELL ({currency.ToString()})");
        try
        {
            await RequestOrder(OrderSide.Sell, currency, takeProfit, stopLoss);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public async Task RequestOrder(OrderSide side, BinanceCurrency currency, decimal takeProfit, decimal stopLoss)
    {
        // Check if bot already has a position open
        if (HasPosition)
        {
            string buyString = Buy ? "Buy" : "Sell";
            throw new Exception($"Bot already has position: {CurrentCurrency} {buyString}");
        }
       
        decimal cost = await GetAvgPrice(currency);
        Enter = cost;

        if (_binanceConfig.Status)
        {
            _logger.LogWarning($"Interacting with finances, as the status is ON)");
            await _binanceClient.UsdFuturesApi.Account.ChangeMarginTypeAsync(currency.ToString(), FuturesMarginType.Isolated);
            await _binanceClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(currency.ToString(), leverage: _binanceConfig.Leverage);
            _logger.LogInformation($"'{currency.ToString()}' Set FuturesMarginType to Isolated, Leverage to {_binanceConfig.Leverage}x");


            // Retrieve bot's current balance and calculate trade amount
            decimal balance = await GetUsdtFuturesBalance();
            decimal balancePercent = balance * _binanceConfig.OrderSizePercent;
            decimal effectiveBalance = balancePercent * _binanceConfig.Leverage;
            decimal amount = Math.Round(effectiveBalance / cost, 2);

            // Log trade details
            _logger.LogInformation($"Trade: {side.ToString()} {amount} {currency.ToString()}: {cost}, TP: {takeProfit}, SL: {stopLoss}");

            // Round take profit and stop loss prices to 2 decimal places
            takeProfit = Math.Round(takeProfit, 2);
            stopLoss = Math.Round(stopLoss, 2);

            _logger.LogWarning($"Placing trade on binance...");
            // Place market order using Binance API
            WebCallResult<Binance.Net.Objects.Models.Futures.BinanceFuturesPlacedOrder> openPositionResult =
                await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(currency.ToString().ToUpper(),
                side, FuturesOrderType.Market, amount);
            OrderId = openPositionResult.Data.Id;

            // Throw exception if order placement was unsuccessful
            if (!openPositionResult.Success)
                throw new Exception(openPositionResult.Error.Message);

            _logger.LogWarning($"Set SL on binance..");
            // Place stop loss order using Binance API
            WebCallResult<Binance.Net.Objects.Models.Futures.BinanceFuturesPlacedOrder> stopLossResult
                = await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(currency.ToString().ToUpper(),
                side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy, FuturesOrderType.StopMarket,
                quantity: null, closePosition: true, stopPrice: stopLoss);

            // If stop loss placement was unsuccessful, try to close the original order and throw exception
            if (!stopLossResult.Success)
            {
                await TryCloseOrder(currency, OrderId);
                throw new Exception(stopLossResult.Error.Message);
            }

            _logger.LogWarning($"Set TP on binance..");
            // Place take profit order using Binance API
            WebCallResult<Binance.Net.Objects.Models.Futures.BinanceFuturesPlacedOrder> takeProfitResult
                = await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(currency.ToString().ToUpper(),
                side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy, FuturesOrderType.TakeProfitMarket,
                quantity: null, closePosition: true, stopPrice: takeProfit);

            // If take profit placement was unsuccessful, try to close the original order and throw exception
            if (!takeProfitResult.Success)
            {
                await TryCloseOrder(currency, OrderId);
                throw new Exception(takeProfitResult.Error.Message);
            }
        }
        else
        {
            _logger.LogWarning($"Do not interact with finances, as the status is OFF");
        }
        // Set bot's position to open and update trade details
        CurrentCurrency = currency;
        Buy = (side == OrderSide.Buy);
        HasPosition = true;
        OrderStarted = DateTime.Now;
    }
}


