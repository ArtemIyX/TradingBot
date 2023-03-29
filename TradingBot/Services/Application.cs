using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingBot.Data;
using static System.Collections.Specialized.BitVector32;

namespace TradingBot.Services
{
    internal class Application
    {
        private readonly ILogger<Application> _logger;
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
            _botConfig = _config.GetSection("Bot").Get<BotConfig>();
            _webhookService = webhookService;
            _telegramBot = telegramBot;
            _brokerService = brokerService;
            _brokerService.TPSLReached += BrokerServiceTpslReached;
            _tradingViewService = tradingViewService;
            _tradingViewService.OnAction += TradingView_OnAction;
            _tradingViewService.OnStop += TradingView_OnStop;
        }

        private async Task BrokerServiceTpslReached(MonitorResult result)
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
                await _telegramBot.SendTP(result.Buy, result.Currency, timeTaken, enterPrice, exitPrice);
            }
            else
            {
                await _telegramBot.SendSL(result.Buy, result.Currency, timeTaken, enterPrice, exitPrice);
            }
        }

        private async Task TradingView_OnAction(object? sender, StrategyAction action)
        {
            _logger.LogInformation($"Trading view action");

            // If we can not cancel and we HAVE position - we can not do anytthing
            if(_brokerService.HasPosition && !_botConfig.Cancel)
            {
                _logger.LogWarning("Can not execute action: Bot already has position");
                _tradingViewService.NotifyFinish();
                return;
            }


            TPSLResult calculatedTPSL = new TPSLResult();
            StrategyAdvancedAction? strategyAdvancedAction = action as StrategyAdvancedAction;
            if (_botConfig.DefaultTakeProfitEnabled)
            {
                calculatedTPSL = await _brokerService.CalculateTPSL(action.Currency, action.Buy, action.Take);
            }
            else
            {
                calculatedTPSL = await _brokerService.CalculateTPSL_Advanced(strategyAdvancedAction.Currency, 
                    strategyAdvancedAction.Buy, strategyAdvancedAction.Take, strategyAdvancedAction.Loss, strategyAdvancedAction.PipSize);
            }
           

            if (calculatedTPSL.Succes)
            {
                decimal entered = _brokerService.Enter;
                TimeSpan timeTaken = DateTime.Now - _brokerService.OrderStarted;
                if (action.Buy)
                {
                    // Has BUY position with same currency
                    if (_brokerService.HasPosition && _brokerService.Buy && _brokerService.CurrentCurrency == action.Currency)
                    {
                        _logger.LogWarning($"Can not execute action: Bot already has BUY position ({_brokerService.CurrentCurrency})");
                    }
                    // Has SELL position with same currency
                    else if(_brokerService.HasPosition && (!_brokerService.Buy) && _brokerService.CurrentCurrency == action.Currency)
                    {
                        //Close SELL Position by market price

                        _monitorSource.Cancel();
                        await _brokerService.ClosePosition();
                        _brokerService.NotifyFinished(action.Currency, false);
                        await _telegramBot.SendCancel(false, action.Currency, timeTaken, entered, calculatedTPSL.Price);

                        //Open BUY Position
                        if (_botConfig.DefaultTakeProfitEnabled)
                        {
                            await TradingView_OnAction(sender, new StrategyAction()
                            {
                                Buy = true,
                                Currency = action.Currency,
                                Take = action.Take,
                            });
                            return;
                        }
                        else
                        {
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
                    }
                    // Doesnt have any position
                    else if (!_brokerService.HasPosition)
                    {
                        await _brokerService.RequestBuy(action.Currency, calculatedTPSL.Take, calculatedTPSL.Loss);

                        _monitorSource = new CancellationTokenSource();
                        Task monitor = _brokerService.StartMonitorCurrency(action.Currency, action.Buy, calculatedTPSL.Take, calculatedTPSL.Loss, _monitorSource.Token);

                        await _telegramBot.SendLong(action.Currency, calculatedTPSL.Price, calculatedTPSL.Take, calculatedTPSL.Loss);
                    }
                    else if(_brokerService.HasPosition)
                    {
                        string buyString = _brokerService.Buy ? "Buy" : "Sell";
                        _logger.LogWarning($"Can not execute action: Bot already has position ({_brokerService.CurrentCurrency} | {buyString})");
                    }
                }
                else
                {
                    // Has SELL position with same currency
                    if (_brokerService.HasPosition && !_brokerService.Buy && _brokerService.CurrentCurrency == action.Currency)
                    {
                        _logger.LogWarning($"Can not execute action: Bot already has SELL position ({_brokerService.CurrentCurrency})");
                    }
                    // Has BUY position with same currency
                    else if (_brokerService.HasPosition && _brokerService.Buy && _brokerService.CurrentCurrency == action.Currency)
                    {
                        //Close BUY Position by market price
                        _monitorSource.Cancel();
                        await _brokerService.ClosePosition();
                        _brokerService.NotifyFinished(action.Currency, true);
                        await _telegramBot.SendCancel(true, action.Currency, timeTaken, entered, calculatedTPSL.Price);

                        //Open SELL Position
                        if (_botConfig.DefaultTakeProfitEnabled)
                        {
                            await TradingView_OnAction(sender, new StrategyAction()
                            {
                                Buy = false,
                                Currency = action.Currency,
                                Take = action.Take,
                            });
                        }
                        else
                        {
                            await TradingView_OnAction(sender, new StrategyAdvancedAction()
                            {
                                Buy = false,
                                Currency = strategyAdvancedAction.Currency,
                                Take = strategyAdvancedAction.Take,
                                Loss = strategyAdvancedAction.Loss,
                                PipSize = strategyAdvancedAction.PipSize
                            });
                        }
                    }
                    // Doesnt have any position
                    else if (!_brokerService.HasPosition)
                    {
                        await _brokerService.RequestSell(action.Currency, calculatedTPSL.Take, calculatedTPSL.Loss);

                        _monitorSource = new CancellationTokenSource();
                        Task monitor = _brokerService.StartMonitorCurrency(action.Currency, action.Buy, calculatedTPSL.Take, calculatedTPSL.Loss, _monitorSource.Token);

                        await _telegramBot.SendShort(action.Currency, calculatedTPSL.Price, calculatedTPSL.Take, calculatedTPSL.Loss);
                    }
                    else if(_brokerService.HasPosition)
                    {
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
            _logger.LogInformation("Pips: ");
            foreach(var pip in _botConfig.Pips)
            {
                _logger.LogInformation($"Pip:[{pip.Currency}\tPipSize: {pip.PipSize}\tTP: {pip.TP}\tSL: {pip.SL}]");
            }
            _webhookService.WebhookReceived += WebhookReceived;
            _webhookService.StartListeningAsync();
            _telegramBot.Start();
            await Task.Delay(-1);
        }

        private void WebhookReceived(object? sender, string e)
        {
            _tradingViewService.ProcessRequest(e);
        }

        public void Finish()
        {
            _webhookService.StopListeting();
        }
    }
}
