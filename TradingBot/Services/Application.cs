using CryptoExchange.Net.CommonObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlX.XDevAPI.Common;
using TradingBot.Data;
using TradingBot.Data.Config;
using TradingBot.Extensions;
using TradingDataBaseLib.Services;

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
                await _tradingActionService.AddTradingAction(result.Buy, result.Currency.ToString(), balance, "TakeProfit");
                await _telegramBot.SendTakeProfit(result.Buy, result.Currency, timeTaken, enterPrice, exitPrice);
            }
            else
            {
                await _tradingActionService.AddTradingAction(result.Buy, result.Currency.ToString(), balance, "StopLoss");
                await _telegramBot.SendStopLoss(result.Buy, result.Currency, timeTaken, enterPrice, exitPrice);
            }
        }

        private async Task TradingView_OnAction(object? sender, StrategyAction action)
        {
            _logger.LogInformation($"Trading view action");

            // If we can not cancel and we HAVE position - we can not do anytthing
            if (_brokerService.HasPosition && !_botConfig.Cancel)
            {
                _logger.LogWarning("Can not execute action: Bot already has position");
                _tradingViewService.NotifyFinish();
                return;
            }


            TakeProfitStopLossResult calculatedTakeProfit;
            StrategyAdvancedAction? strategyAdvancedAction = action as StrategyAdvancedAction;

            if (strategyAdvancedAction == null)
                throw new Exception("Request pip take profit, but strategy advanced action is null");
            calculatedTakeProfit = await _brokerService.CalculateTPSL_Advanced(
                strategyAdvancedAction.Currency,
                strategyAdvancedAction.Buy,
                strategyAdvancedAction.Take,
                strategyAdvancedAction.Loss,
                strategyAdvancedAction.PipSize);


            if (calculatedTakeProfit.Success)
            {
                decimal entered = _brokerService.Enter;
                TimeSpan timeTaken = DateTime.Now - _brokerService.OrderStarted;
                if (action.Buy)
                {
                    // Check if the bot already has a BUY position with the same currency
                    if (_brokerService.HasPosition && _brokerService.Buy &&
                        _brokerService.CurrentCurrency == action.Currency)
                    {
                        // Log a warning message and do not execute the action
                        _logger.LogWarning(
                            $"Can not execute action: Bot already has BUY position ({_brokerService.CurrentCurrency})");
                    }
                    // Check if the bot already has a SELL position with the same currency
                    else if (_brokerService.HasPosition && (!_brokerService.Buy) &&
                             _brokerService.CurrentCurrency == action.Currency &&
                             _botConfig.Cancel)
                    {
                        // Close the SELL position by market price
                        _logger.LogInformation("Cancel: Close SELL position by market price");
                        _monitorSource.Cancel();
                        await _brokerService.ClosePosition();

                        // Get the current balance and add a trading action to the history
                        decimal balance = await _brokerService.GetUsdtFuturesBalance();
                        await _tradingActionService.AddTradingAction(false, action.Currency.ToString(), balance, "Cancel");

                        // Notify that the action has finished and send a Telegram message
                        _brokerService.NotifyFinished(action.Currency, false);
                        await _telegramBot.SendCancel(false, action.Currency, timeTaken, entered, calculatedTakeProfit.Price);

                        // Check if a pip take profit is requested and open a new BUY position
                        if (strategyAdvancedAction == null)
                            throw new Exception("Request pip take profit, but strategy advanced action is null");

                        if (_botConfig.Reverse)
                        {
                            _logger.LogInformation("Trend reverse: Open BUY position");
                            await TradingView_OnAction(sender, new StrategyAdvancedAction()
                            {
                                Buy = true,
                                Currency = strategyAdvancedAction.Currency,
                                Take = strategyAdvancedAction.Take,
                                Loss = strategyAdvancedAction.Loss,
                                PipSize = strategyAdvancedAction.PipSize
                            });
                        }
                        return;
                    }
                    // Check if the bot does not have any position and request a BUY position
                    else if (!_brokerService.HasPosition)
                    {
                        _logger.LogInformation("Bot is free, reqeusting BUY position");
                        await _brokerService.RequestBuy(action.Currency, calculatedTakeProfit.Take, calculatedTakeProfit.Loss);
                        await _telegramBot.SendLong(action.Currency, calculatedTakeProfit.Price, calculatedTakeProfit.Take, calculatedTakeProfit.Loss);
                    }
                    // Check if the bot already has a position and log a warning message
                    else if (_brokerService.HasPosition)
                    {
                        string buyString = _brokerService.Buy ? "Buy" : "Sell";
                        _logger.LogWarning(
                            $"Can not execute action: Bot already has position ({_brokerService.CurrentCurrency} | {buyString})");
                    }
                }
                else
                {
                    // Check if bot has a SELL position with the same currency
                    if (_brokerService.HasPosition && !_brokerService.Buy && _brokerService.CurrentCurrency == action.Currency)
                    {
                        _logger.LogWarning($"Can not execute action: Bot already has SELL position ({_brokerService.CurrentCurrency})");
                    }
                    // Check if bot has a BUY position with the same currency
                    else if (_brokerService.HasPosition 
                        && _brokerService.Buy 
                        && _brokerService.CurrentCurrency == action.Currency
                        && _botConfig.Cancel)
                    {
                        // Close the BUY position by market price
                        _logger.LogInformation("Trend reverse: Close BUY position by market price");
                        _monitorSource.Cancel();
                        await _brokerService.ClosePosition();

                        // Get current balance and add trading action
                        decimal balance = await _brokerService.GetUsdtFuturesBalance();
                        await _tradingActionService.AddTradingAction(true, action.Currency.ToString(), balance, "Cancel");

                        // Notify that the action has finished and send a message to Telegram
                        _brokerService.NotifyFinished(action.Currency, true);
                        await _telegramBot.SendCancel(true, action.Currency, timeTaken, entered, calculatedTakeProfit.Price);

                        // Check if the advanced action is null, throw an exception if it is
                        if (strategyAdvancedAction == null)
                            throw new Exception("Request pip take profit, but strategy advanced action is null");

                        // Open a SELL position
                        if (_botConfig.Reverse)
                        {
                            _logger.LogInformation("Trend reverse: Open SELL position");
                            await TradingView_OnAction(sender, new StrategyAdvancedAction()
                            {
                                Buy = false,
                                Currency = strategyAdvancedAction.Currency,
                                Take = strategyAdvancedAction.Take,
                                Loss = strategyAdvancedAction.Loss,
                                PipSize = strategyAdvancedAction.PipSize
                            });
                        }
                        return;
                    }
                    // Check if bot doesn't have any position
                    else if (!_brokerService.HasPosition)
                    {
                        _logger.LogInformation("Bot is free, requesting SELL position");
                        await _brokerService.RequestSell(action.Currency, calculatedTakeProfit.Take, calculatedTakeProfit.Loss);

                        // Send a message to Telegram
                        await _telegramBot.SendShort(action.Currency, calculatedTakeProfit.Price, calculatedTakeProfit.Take, calculatedTakeProfit.Loss);
                    }
                    else if (_brokerService.HasPosition)
                    {
                        // If bot already has a position, log a warning and do nothing
                        string buyString = _brokerService.Buy ? "Buy" : "Sell";
                        _logger.LogWarning($"Can not execute action: Bot already has position ({_brokerService.CurrentCurrency} | {buyString})");
                    }
                }
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
            Console.WriteLine("Pips: ");
            foreach (var pip in _botConfig.Pips)
            {
                Console.WriteLine($"Pip:[{pip.Currency}\tPipSize: {pip.PipSize}\tTP: {pip.Tp}\tSL: {pip.Sl}]");
            }
            _webhookService.WebhookReceived += WebhookReceived;
            // ReSharper disable once UnusedVariable
            Task webhookListeningTask = _webhookService.StartListeningAsync();
            Task telegramBotTask = _telegramBot.Start();
            await _brokerService.ConnectToStream();
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