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
    private async Task<decimal> GetAvgPrice(BinanceCurrency currency)
    {
        // Call the Binance API to get the current average price for the currency
        var res = await _binanceClient.SpotApi.ExchangeData.GetCurrentAvgPriceAsync(currency.ToString().ToUpper());

        // If the API call was unsuccessful, throw an exception with the error message
        if (!res.Success)
            throw new Exception(res.Error.Message);

        // Otherwise, return the average price as a decimal
        return res.Data.Price;
    }

    // This method asynchronously attempts to cancel an open order for the specified currency and order ID.
    // It takes a BinanceCurrency object and a long integer representing the order ID as parameters.
    private async Task TryCloseOrder(BinanceCurrency currency, long order)
    {
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
        }
    }

    public async Task RequestBuy(BinanceCurrency currency, decimal enterPrice, decimal takeProfit, decimal stopLoss)
    {
        await RequestOrder(OrderSide.Buy, currency, enterPrice, takeProfit, stopLoss);
    }

    public async Task RequestSell(BinanceCurrency currency, decimal enterPrice, decimal takeProfit, decimal stopLoss)
    {
        await RequestOrder(OrderSide.Sell, currency, enterPrice, takeProfit, stopLoss);
    }

    public async Task RequestOrder(OrderSide side, BinanceCurrency currency, decimal enterPrice, decimal takeProfit, decimal stopLoss)
    {
        // Check if bot already has a position open
        if (HasPosition)
        {
            string buyString = Buy ? "Buy" : "Sell";
            throw new Exception($"Bot already has position: {CurrentCurrency} {buyString}");
        }

        // Log trade details
        _logger.LogInformation($"{side.ToString()} {currency.ToString()}: {enterPrice}, TP: {takeProfit}, SL: {stopLoss}");

        // Retrieve bot's current balance and calculate trade amount
        decimal balance = await GetUsdtFuturesBalance();
        decimal cost = await GetAvgPrice(currency);
        Enter = cost;
        decimal balancePercent = _binanceConfig.Percent * balance;
        decimal amount = Math.Round(balancePercent / cost, 2);

        // Round take profit and stop loss prices to 2 decimal places
        takeProfit = Math.Round(takeProfit, 2);
        stopLoss = Math.Round(stopLoss, 2);

        // Place market order using Binance API
        WebCallResult<Binance.Net.Objects.Models.Futures.BinanceFuturesPlacedOrder> openPositionResult =
            await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(currency.ToString().ToUpper(),
            side, FuturesOrderType.Market, amount);
        OrderId = openPositionResult.Data.Id;

        // Throw exception if order placement was unsuccessful
        if (!openPositionResult.Success)
            throw new Exception(openPositionResult.Error.Message);

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

        // Set bot's position to open and update trade details
        CurrentCurrency = currency;
        Enter = enterPrice;
        Buy = side == OrderSide.Buy;
        HasPosition = true;
    }
}


