using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TradingBot.Data;

namespace TradingBot.Services;

public enum BinanceCurrency
{
    BTCUSDT,
    ETHUSDT,
    OPUSDT,
    KAVAUSDT,
    AVAXUSDT,
    FLOWUSDT,
    FTMUSDT,
    RVNUSDT,
    INJUSDT,
    SOLUSDT,
    CFXUSDT,
    BNXUSDT,
    HOTUSDT,
    XMRUSDT,
    LQTYUSDT,
    TRUUSDT,
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
}

internal class BinanceService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookService> _logger;
    private readonly BinanceConfig? _binanceConfig;
    private BinanceClient _binanceClient;

    public BinanceCurrency CurrentCurrency { get; private set; }
    public bool HasPosition { get; private set; }
    public bool Buy { get; set; }
    public decimal Enter { get; set; }
    public long OrderId { get; set; }

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

    public async Task<decimal> GetUsdtFuturesBalance()
    {
        WebCallResult<IEnumerable<Binance.Net.Objects.Models.Futures.BinanceFuturesAccountBalance>> res 
            = await _binanceClient.UsdFuturesApi.Account.GetBalancesAsync();
        if (!res.Success)
            throw new Exception(res.Error.Message);
        
        foreach(var data in res.Data)
        {
            if(data.Asset == "USDT")
            {
                return data.AvailableBalance;
            }
        }
        throw new Exception("Can not find USDT Asset");
    }

    /*private void Test()
    {
        var balance = await GetUsdtFuturesBalance();
        var btc = await _binanceClient.SpotApi.ExchangeData.GetCurrentAvgPriceAsync("BTCUSDT");
        var percent_1 = 0.01m * balance;
        var amount = Math.Round(percent_1 / btc.Data.Price, 2);

        var takeProfit = Math.Round(btc.Data.Price * 1.01m, 2);
        var stopLoss = Math.Round(btc.Data.Price * 0.99m, 2);

        _logger.LogInformation($"Btc is {btc.Data.Price}\nTakeProfit:{takeProfit}\nStopLoss:{stopLoss}\nAmount:{amount}");
        var openPositionResult = await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync("BTCUSDT", OrderSide.Buy, FuturesOrderType.Market, amount);
        if (!openPositionResult.Success)
            _logger.LogError(openPositionResult.Error.Message);
        else
            _logger.LogInformation(openPositionResult.Data.Quantity.ToString());

        await Task.Delay(1000);
        var stopLossResult = await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync("BTCUSDT", OrderSide.Sell, FuturesOrderType.StopMarket,
            quantity: null, closePosition: true, stopPrice: stopLoss);
        if (!stopLossResult.Success)
            _logger.LogError(stopLossResult.Error.Message);
        else
            _logger.LogInformation(stopLossResult.Data.Pair);

        await Task.Delay(1000);
        var takeProfitResult = await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync("BTCUSDT", OrderSide.Sell, FuturesOrderType.TakeProfitMarket,
            quantity: null, closePosition: true, stopPrice: takeProfit);
        if (!takeProfitResult.Success)
            _logger.LogError(takeProfitResult.Error.Message);
        else
            _logger.LogInformation(takeProfitResult.Data.Status.ToString());
        return false;
    }*/

    private async Task<decimal> GetAvgPrice(BinanceCurrency currency)
    {
        var res = await _binanceClient.SpotApi.ExchangeData.GetCurrentAvgPriceAsync(currency.ToString().ToUpper());
        if (!res.Success)
            throw new Exception(res.Error.Message);

        return res.Data.Price;
    }

    private async Task TryCloseOrder(BinanceCurrency currency, long order)
    {
        _logger.LogInformation($"Canceling order '{order}' ({currency.ToString()})");
        var orderData = await _binanceClient.UsdFuturesApi.Trading.GetOrderAsync(currency.ToString(), order);
        if(orderData.Success)
        {
            var cancelOrderData = await _binanceClient.UsdFuturesApi.Trading.CancelOrderAsync(currency.ToString(), order);
            if(!cancelOrderData.Success) 
            {
                _logger.LogError($"Can not cancel order {order} ({currency.ToString()})");
                return;
            }
            _logger.LogInformation($"Canceled order '{order}'");
        }
        else
        {
            _logger.LogWarning($"Tried to close order '{order}' with {currency.ToString().ToUpper()}, but can not find this order");
        }
    }

    public async Task RequestBuy(BinanceCurrency currency, decimal enterPrice, decimal takeProfit, decimal stopLoss)
    {
        if(HasPosition)
        {
            string buyString = Buy ? "Buy" : "Sell";
            throw new Exception($"Bot already has position: {CurrentCurrency} {buyString}");
        }

        _logger.LogInformation($"Buy {currency.ToString()}: {enterPrice}, TP: {takeProfit}, SL: {stopLoss}");
        decimal balance = await GetUsdtFuturesBalance();
        decimal cost = await GetAvgPrice(currency);
        Enter = cost;
        decimal balancePercent = _binanceConfig.Percent * balance;
        decimal amount = Math.Round(balancePercent / cost, 2);

        takeProfit = Math.Round(takeProfit, 2);
        stopLoss = Math.Round(stopLoss, 2);

        WebCallResult<Binance.Net.Objects.Models.Futures.BinanceFuturesPlacedOrder> openPositionResult = 
            await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(currency.ToString().ToUpper(), 
            OrderSide.Buy, FuturesOrderType.Market, amount);

        OrderId = openPositionResult.Data.Id;

        if (!openPositionResult.Success)
            throw new Exception(openPositionResult.Error.Message);

        WebCallResult<Binance.Net.Objects.Models.Futures.BinanceFuturesPlacedOrder> stopLossResult 
            = await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(currency.ToString().ToUpper(),
            OrderSide.Sell, FuturesOrderType.StopMarket,
            quantity: null, closePosition: true, stopPrice: stopLoss);

        if(!stopLossResult.Success)
        {
            await TryCloseOrder(currency, OrderId);
            throw new Exception(stopLossResult.Error.Message);
        }

        WebCallResult<Binance.Net.Objects.Models.Futures.BinanceFuturesPlacedOrder> takeProfitResult 
            = await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(currency.ToString().ToUpper(), 
            OrderSide.Sell, FuturesOrderType.TakeProfitMarket,
            quantity: null, closePosition: true, stopPrice: takeProfit);

        if (!takeProfitResult.Success)
        {
            await TryCloseOrder(currency, OrderId);
            throw new Exception(takeProfitResult.Error.Message);
        }

        CurrentCurrency = currency;
        Enter = enterPrice;
        Buy = true;
        HasPosition = true;
    }
}


