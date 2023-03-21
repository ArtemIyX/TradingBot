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
public struct MonitorResult
{
    public BinanceCurrency Currency { get; set; }
    public bool Take { get; set; }
    public bool Buy { get; set; }
    public decimal Price { get; set; }
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
        int need = 20;
        while(true)
        {
            decimal current = await GetAvgPrice(currency);
            i++;
            if (i == need)
            {
                _logger.LogInformation($"Monitoring {currency}: {current} (TP: {take}, SP: {stop})");
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
        var res = await _binanceClient.SpotApi.ExchangeData.GetCurrentAvgPriceAsync(currency.ToString().ToUpper());

        // If the API call was unsuccessful, throw an exception with the error message
        if (!res.Success)
            throw new Exception(res.Error.Message);

        // Otherwise, return the average price as a decimal
        return res.Data.Price;
    }

    public async Task<(decimal Price, decimal Take, decimal Loss)> CalculateTPSL(BinanceCurrency currency, bool buy, decimal TakePercent, decimal LossPercent)
    {
        // Determine where you've entered and in what direction
        /* longStop = strategy.position_avg_price * (1 - stopPer)
         shortStop = strategy.position_avg_price * (1 + stopPer)
         shortTake = strategy.position_avg_price * (1 - takePer)
         longTake = strategy.position_avg_price * (1 + takePer)*/
        try
        {
            decimal avgPprice = await GetAvgPrice(currency);
            if (buy)
            {
                decimal longTake = avgPprice * (1 + TakePercent);
                decimal longStop = avgPprice * (1 - LossPercent);
                return (Price: avgPprice, Take: longTake, Loss: longStop);
            }
            else
            {
                decimal shortTake = avgPprice * (1 - TakePercent);
                decimal shortStop = avgPprice * (1 + LossPercent);
                return (Price: avgPprice, Take: shortTake, Loss: shortStop);
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex.Message);
            return (0.0m, 0.0m, 0.0m);
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
            _logger.LogWarning($"Do not interact with finances, as the status is OFF)");
        }
        // Set bot's position to open and update trade details
        CurrentCurrency = currency;
        Buy = (side == OrderSide.Buy);
        HasPosition = true;
        OrderStarted = DateTime.Now;
    }
}


