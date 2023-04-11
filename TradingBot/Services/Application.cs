using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System;
using TradingBot.Data;
using TradingBot.Data.Config;
using TradingBot.Extensions;
using TradingDataBaseLib.Services;
using Newtonsoft.Json;

namespace TradingBot.Services
{
    internal class Application
    {
        private readonly ILogger<Application> _logger;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly IConfiguration _config;
        private readonly WebhookService _webhookService;
        private readonly TelegramService _telegramBot;
        private readonly BrokerService _brokerService;
        private readonly TradingViewService _tradingViewService;
        private readonly TradingActionService _tradingActionService;
        private readonly BotConfig _botConfig;
        private readonly DataBaseConfig _dbConfig;
        private CancellationTokenSource _monitorSource;


        public Application(
            ILogger<Application> logger,
            IConfiguration config,
            WebhookService webhookService,
            TelegramService telegramBot,
            BrokerService brokerService,
            TradingViewService tradingViewService,
            TradingActionService tradingActionService)
        {
            _monitorSource = new CancellationTokenSource();
            _logger = logger;
            _config = config;
            _botConfig = _config.GetSection("Bot").Get<BotConfig>() ??
                         throw new InvalidOperationException("Can not get 'Bot' settings");
            _webhookService = webhookService;
            _telegramBot = telegramBot;
            _tradingActionService = tradingActionService;
            _dbConfig = config.GetSection("DataBase").Get<DataBaseConfig>() ?? throw new Exception("Can not get 'DataBase' appsettings section");

            _brokerService = brokerService;
            _brokerService.TpSlReached += BrokerServiceTpSlReached;

            _tradingViewService = tradingViewService;
            _tradingViewService.OnAction += TradingView_OnAction;
            _tradingViewService.OnStop += TradingView_OnStop;
        }

        private async Task BrokerServiceTpSlReached(MonitorResult result)
        {
            string takeProfitStr = result.Take ? "TP" : "SL";
            string longStr = result.Buy ? "Long" : "Short";
            _logger.LogInformation($"{takeProfitStr}: {result.Currency} / {longStr} ({result.Price})");

            TimeSpan timeTaken = DateTime.Now - _brokerService.OrderStarted;
            decimal exitPrice = result.Price;
            decimal enterPrice = _brokerService.Enter;

            decimal balance = await _brokerService.GetUsdtFuturesBalance();

            // Notify binance service
            _brokerService.NotifyFinished(result.Currency, result.Buy);
            if (result.Take)
            {
                if (_dbConfig.UseDb)
                    await _tradingActionService.AddTradingAction(result.Buy, result.Currency.ToString(), balance, "TakeProfit");
                await _telegramBot.SendTakeProfit(result.Buy, result.Currency, timeTaken, enterPrice, exitPrice);
            }
            else
            {
                if (_dbConfig.UseDb)
                    await _tradingActionService.AddTradingAction(result.Buy, result.Currency.ToString(), balance, "StopLoss");
                await _telegramBot.SendStopLoss(result.Buy, result.Currency, timeTaken, enterPrice, exitPrice);
            }
        }
        private async Task ProcessBuyAction(CryptoCurrency currency)
        {
            
            // Check if the bot already has a BUY position with the same currency
            if (_brokerService.HasPosition && _brokerService.Buy &&
                _brokerService.CurrentCurrency == currency)
            {
                // Log a warning message and do not execute the action
                _logger.LogWarning(
                    $"Can not execute action: Bot already has BUY position ({_brokerService.CurrentCurrency})");
            }
            // Check if the bot already has a SELL position with the same currency
            else if (_brokerService.HasPosition && (!_brokerService.Buy) &&
                     _brokerService.CurrentCurrency == currency &&
                     _botConfig.Cancel)
            {
                // Close the SELL position by market price
                _logger.LogInformation("Cancel: Close SELL position by market price");
                _monitorSource.Cancel();
                await _brokerService.ClosePosition();

                // Get the current balance and add a trading action to the history
                decimal balance = await _brokerService.GetUsdtFuturesBalance();
                if (_dbConfig.UseDb)
                    await _tradingActionService.AddTradingAction(false, currency.ToString(), balance, "Cancel");

                // Notify that the action has finished and send a Telegram message
                decimal entered = _brokerService.Enter;
                TimeSpan timeTaken = DateTime.Now - _brokerService.OrderStarted;
                _brokerService.NotifyFinished(currency, false);
                decimal currentPrice = await _brokerService.GetAvgPrice(currency);
                await _telegramBot.SendCancel(false, currency, timeTaken, entered, currentPrice);
                
                if (_botConfig.Reverse)
                {
                    _logger.LogInformation("Trend reverse: Open BUY position");

                    await TradingView_OnAction(_tradingViewService, new StrategyAdvancedAction()
                    {
                        Buy = true,
                        Currency = currency,
                    });
                }
                return;
            }
            // Check if the bot does not have any position and request a BUY position
            else if (!_brokerService.HasPosition)
            {
                _logger.LogInformation("Bot is free, reqeusting BUY position");
                decimal currentPrice = await _brokerService.GetAvgPrice(currency);
                decimal? tp = await _brokerService.CalculateTakeProfit(Bybit.Net.Enums.OrderSide.Buy, currency, currentPrice);
                decimal? sl = await _brokerService.CalculateStopLoss(Bybit.Net.Enums.OrderSide.Buy, currency, currentPrice);
                await _brokerService.RequestBuy(currency, tp, sl);
                await _telegramBot.SendLong(currency, currentPrice, tp, sl);
            }
            // Check if the bot already has a position and log a warning message
            else if (_brokerService.HasPosition)
            {
                string buyString = _brokerService.Buy ? "Buy" : "Sell";
                _logger.LogWarning(
                    $"Can not execute action: Bot already has position ({_brokerService.CurrentCurrency} | {buyString})");
            }
        }

        private async Task ProcessSellAction(CryptoCurrency currency)
        {
            // Check if bot has a SELL position with the same currency
            if (_brokerService.HasPosition && !_brokerService.Buy && _brokerService.CurrentCurrency == currency)
            {
                _logger.LogWarning($"Can not execute action: Bot already has SELL position ({_brokerService.CurrentCurrency})");
            }
            // Check if bot has a BUY position with the same currency
            else if (_brokerService.HasPosition
                && _brokerService.Buy
                && _brokerService.CurrentCurrency == currency
                && _botConfig.Cancel)
            {
                // Close the BUY position by market price
                _logger.LogInformation("Trend reverse: Close BUY position by market price");
                _monitorSource.Cancel();
                await _brokerService.ClosePosition();

                // Get current balance and add trading action
                decimal balance = await _brokerService.GetUsdtFuturesBalance();
                if (_dbConfig.UseDb)
                    await _tradingActionService.AddTradingAction(true, currency.ToString(), balance, "Cancel");

                // Notify that the action has finished and send a message to Telegram
                decimal entered = _brokerService.Enter;
                TimeSpan timeTaken = DateTime.Now - _brokerService.OrderStarted;
                _brokerService.NotifyFinished(currency, true);
                decimal currentPrice = await _brokerService.GetAvgPrice(currency);
                await _telegramBot.SendCancel(true, currency, timeTaken, entered, currentPrice);

                // Check if the advanced action is null, throw an exception if it is;

                // Open a SELL position
                if (_botConfig.Reverse)
                {
                    _logger.LogInformation("Trend reverse: Open SELL position");
                    await TradingView_OnAction(_tradingViewService, new StrategyAction()
                    {
                        Buy = false,
                        Currency = currency,
                    });
                }
                return;
            }
            // Check if bot doesn't have any position
            else if (!_brokerService.HasPosition)
            {
                _logger.LogInformation("Bot is free, requesting SELL position");
                decimal currentPrice = await _brokerService.GetAvgPrice(currency);
                decimal? tp = await _brokerService.CalculateTakeProfit(Bybit.Net.Enums.OrderSide.Sell, currency, currentPrice);
                decimal? sl = await _brokerService.CalculateStopLoss(Bybit.Net.Enums.OrderSide.Sell, currency, currentPrice);
                await _brokerService.RequestSell(currency, tp, sl);

                // Send a message to Telegram
                await _telegramBot.SendShort(currency, currentPrice, tp, sl);
            }
            else if (_brokerService.HasPosition)
            {
                // If bot already has a position, log a warning and do nothing
                string buyString = _brokerService.Buy ? "Buy" : "Sell";
                _logger.LogWarning($"Can not execute action: Bot already has position ({_brokerService.CurrentCurrency} | {buyString})");
            }
        }

        private async Task TradingView_OnAction(object? sender, StrategyAction action)
        {
            _logger.LogInformation($"Trading view action");

            if (action.Buy)
            {
                await ProcessBuyAction(action.Currency);
            }
            else
            {
                await ProcessSellAction(action.Currency);
            }


            _tradingViewService.NotifyFinish();
        }

        private async Task TradingView_OnStop(object? sender, StrategyStop stop)
        {
            _logger.LogInformation($"Trading view stop");
            _tradingViewService.NotifyFinish();
            return;
        }

        public async Task Start()
        {
            _logger.LogInformation("WELLSAIK ALERTS");
            await ServiceExtensions.SyncTime(_logger);

            if (_dbConfig.UseDb)
                await _tradingActionService.EnsureCreatedDB();

            Console.WriteLine("Pips: ");
            foreach (BinancePip pip in _botConfig.Pips)
            {
                Console.WriteLine($"Pip:[{pip.Currency}\tPipSize: {pip.PipSize}\tTP: {pip.Tp}\tSL: {pip.Sl}]");
            }
            _webhookService.WebhookReceived += WebhookReceived;
            // ReSharper disable once UnusedVariable
            Task webhookListeningTask = _webhookService.StartListeningAsync();
            Task telegramBotTask = _telegramBot.Start();
            Task connectToStream = _brokerService.ConnectToStream();
            await Task.Delay(-1);
        }

        private void WebhookReceived(object? sender, string e)
        {
            _tradingViewService.ProcessRequest(e);
        }

        public void Finish()
        {
            _webhookService.StopListening();
        }
    }
}