using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradingBot.Data;

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
        private readonly BotConfig _botConfig;
        private CancellationTokenSource _monitorSource;


        public Application(
            ILogger<Application> logger,
            IConfiguration config,
            WebhookService webhookService,
            TelegramService telegramBot,
            BrokerService brokerService,
            TradingViewService tradingViewService)
        {
            _monitorSource = new CancellationTokenSource();
            _logger = logger;
            _config = config;
            _botConfig = _config.GetSection("Bot").Get<BotConfig>() ??
                         throw new InvalidOperationException("Can not get 'Bot' settings");
            _webhookService = webhookService;
            _telegramBot = telegramBot;
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

            // Notify binance service
            _brokerService.NotifyFinished(result.Currency, result.Buy);
            if (result.Take)
            {
                await _telegramBot.SendTakeProfit(result.Buy, result.Currency, timeTaken, enterPrice, exitPrice);
            }
            else
            {
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
                    // Has BUY position with same currency
                    if (_brokerService.HasPosition && _brokerService.Buy &&
                        _brokerService.CurrentCurrency == action.Currency)
                    {
                        _logger.LogWarning(
                            $"Can not execute action: Bot already has BUY position ({_brokerService.CurrentCurrency})");
                    }
                    // Has SELL position with same currency
                    else if (_brokerService.HasPosition && (!_brokerService.Buy) &&
                             _brokerService.CurrentCurrency == action.Currency)
                    {
                        //Close SELL Position by market price
                        _logger.LogInformation("Trend reverse: Close SELL position by market price");
                        _monitorSource.Cancel();
                        await _brokerService.ClosePosition();
                        _brokerService.NotifyFinished(action.Currency, false);
                        await _telegramBot.SendCancel(false, action.Currency, timeTaken, entered,
                            calculatedTakeProfit.Price);


                        if (strategyAdvancedAction == null)
                            throw new Exception("Request pip take profit, but strategy advanced action is null");
                        _logger.LogInformation("Trend reverse: Open BUY position");
                        await TradingView_OnAction(sender, new StrategyAdvancedAction()
                        {
                            Buy = true,
                            Currency = strategyAdvancedAction.Currency,
                            Take = strategyAdvancedAction.Take,
                            Loss = strategyAdvancedAction.Loss,
                            PipSize = strategyAdvancedAction.PipSize
                        });
                        return;
                    }
                    // Doesnt have any position
                    else if (!_brokerService.HasPosition)
                    {
                        _logger.LogInformation("Bot is free, reqeusting BUY position");
                        await _brokerService.RequestBuy(action.Currency, calculatedTakeProfit.Take,
                            calculatedTakeProfit.Loss);

                        _monitorSource = new CancellationTokenSource();
                        // ReSharper disable once UnusedVariable
                        Task monitor = _brokerService.StartMonitorCurrency(action.Currency, action.Buy,
                            calculatedTakeProfit.Take, calculatedTakeProfit.Loss, _monitorSource.Token);

                        await _telegramBot.SendLong(action.Currency, calculatedTakeProfit.Price,
                            calculatedTakeProfit.Take, calculatedTakeProfit.Loss);
                    }
                    else if (_brokerService.HasPosition)
                    {
                        string buyString = _brokerService.Buy ? "Buy" : "Sell";
                        _logger.LogWarning(
                            $"Can not execute action: Bot already has position ({_brokerService.CurrentCurrency} | {buyString})");
                    }
                }
                else
                {
                    // Has SELL position with same currency
                    if (_brokerService.HasPosition && !_brokerService.Buy &&
                        _brokerService.CurrentCurrency == action.Currency)
                    {
                        _logger.LogWarning(
                            $"Can not execute action: Bot already has SELL position ({_brokerService.CurrentCurrency})");
                    }
                    // Has BUY position with same currency
                    else if (_brokerService.HasPosition && _brokerService.Buy &&
                             _brokerService.CurrentCurrency == action.Currency)
                    {
                        //Close BUY Position by market price
                        _logger.LogInformation("Trend reverse: Close BUY position by market price");
                        _monitorSource.Cancel();
                        await _brokerService.ClosePosition();
                        _brokerService.NotifyFinished(action.Currency, true);
                        await _telegramBot.SendCancel(true, action.Currency, timeTaken, entered,
                            calculatedTakeProfit.Price);
                        
                        if (strategyAdvancedAction == null)
                            throw new Exception("Request pip take profit, but strategy advanced action is null");

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
                    // Doesnt have any position
                    else if (!_brokerService.HasPosition)
                    {
                        _logger.LogInformation("Bot is free, reqeusting SELL position");
                        await _brokerService.RequestSell(action.Currency, calculatedTakeProfit.Take,
                            calculatedTakeProfit.Loss);

                        _monitorSource = new CancellationTokenSource();
                        // ReSharper disable once UnusedVariable
                        Task monitor = _brokerService.StartMonitorCurrency(action.Currency, action.Buy,
                            calculatedTakeProfit.Take, calculatedTakeProfit.Loss, _monitorSource.Token);

                        await _telegramBot.SendShort(action.Currency, calculatedTakeProfit.Price,
                            calculatedTakeProfit.Take, calculatedTakeProfit.Loss);
                    }
                    else if (_brokerService.HasPosition)
                    {
                        string buyString = _brokerService.Buy ? "Buy" : "Sell";
                        _logger.LogWarning(
                            $"Can not execute action: Bot already has position ({_brokerService.CurrentCurrency} | {buyString})");
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
            _logger.LogInformation("Pips: ");
            foreach (var pip in _botConfig.Pips)
            {
                _logger.LogInformation($"Pip:[{pip.Currency}\tPipSize: {pip.PipSize}\tTP: {pip.Tp}\tSL: {pip.Sl}]");
            }

            _webhookService.WebhookReceived += WebhookReceived;
            // ReSharper disable once UnusedVariable
            Task webhookListeningTask = _webhookService.StartListeningAsync();
            Task telegramBotTask = _telegramBot.Start();
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