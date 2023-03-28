﻿using Microsoft.Extensions.Configuration;
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
        private readonly BinanceService _binanceService;
        private readonly TradingViewService _tradingViewService;
        private readonly BotConfig _botConfig;
        private CancellationTokenSource _monitorSource;


        public Application(
            ILogger<Application> logger, 
            IConfiguration config,
            WebhookService webhookService,
            TelegramService telegramBot, 
            BinanceService binanceService,
            TradingViewService tradingViewService)
        {
            _monitorSource = new CancellationTokenSource();
            _logger = logger;
            _config = config;
            _botConfig = _config.GetSection("Bot").Get<BotConfig>();
            _webhookService = webhookService;
            _telegramBot = telegramBot;
            _binanceService = binanceService;
            _binanceService.TPSLReached += BinanceService_TPSLReached;
            _tradingViewService = tradingViewService;
            _tradingViewService.OnAction += TradingView_OnAction;
            _tradingViewService.OnStop += TradingView_OnStop;
        }

        private async Task BinanceService_TPSLReached(MonitorResult result)
        {
            string takeProfitStr = result.Take ? "TP" : "SL";
            string longStr = result.Buy ? "Long" : "Short";
            _logger.LogInformation($"{takeProfitStr}: {result.Currency} / {longStr} ({result.Price})");

            TimeSpan timeTaken = DateTime.Now - _binanceService.OrderStarted;
            decimal exitPrice = result.Price;
            decimal enterPrice = _binanceService.Enter;

            // Notify binance service
            _binanceService.NotifyFinished(result.Currency, result.Buy);
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
            if(_binanceService.HasPosition && !_botConfig.Cancel)
            {
                _logger.LogWarning("Can not execute action: Bot already has position");
                _tradingViewService.NotifyFinish();
                return;
            }


            TPSLResult calculatedTPSL = 
                await _binanceService.CalculateTPSL(action.Currency, action.Buy, action.Take);
            decimal entered = _binanceService.Enter;
            TimeSpan timeTaken = DateTime.Now - _binanceService.OrderStarted;

            if (calculatedTPSL.Succes)
            {
                if (action.Buy)
                {
                    // Has BUY position with same currency
                    if (_binanceService.HasPosition && _binanceService.Buy && _binanceService.CurrentCurrency == action.Currency)
                    {
                        _logger.LogWarning($"Can not execute action: Bot already has BUY position ({_binanceService.CurrentCurrency})");
                    }
                    // Has SELL position with same currency
                    else if(_binanceService.HasPosition && (!_binanceService.Buy) && _binanceService.CurrentCurrency == action.Currency)
                    {
                        //Close SELL Position by market price

                        _monitorSource.Cancel();
                        await _binanceService.ClosePosition();
                        _binanceService.NotifyFinished(action.Currency, false);
                        await _telegramBot.SendCancel(false, action.Currency, timeTaken, entered, calculatedTPSL.Price);

                        //Open BUY Position
                        await TradingView_OnAction(sender, new StrategyAction()
                        {
                            Buy = true,
                            Currency = action.Currency,
                            Take = action.Take,
                            Loss = action.Loss
                        });
                    }
                    // Doesnt have any position
                    else if (!_binanceService.HasPosition)
                    {
                        await _binanceService.RequestBuy(action.Currency, calculatedTPSL.Take, calculatedTPSL.Loss);

                        _monitorSource = new CancellationTokenSource();
                        Task monitor = _binanceService.StartMonitorTPSL(action.Currency, action.Buy, calculatedTPSL.Take, calculatedTPSL.Loss, _monitorSource.Token);

                        await _telegramBot.SendLong(action.Currency, calculatedTPSL.Price, calculatedTPSL.Take, calculatedTPSL.Loss);
                    }
                    else if(_binanceService.HasPosition)
                    {
                        string buyString = _binanceService.Buy ? "Buy" : "Sell";
                        _logger.LogWarning($"Can not execute action: Bot already has position ({_binanceService.CurrentCurrency} | {buyString})");
                    }
                }
                else
                {
                    // Has SELL position with same currency
                    if (_binanceService.HasPosition && !_binanceService.Buy && _binanceService.CurrentCurrency == action.Currency)
                    {
                        _logger.LogWarning($"Can not execute action: Bot already has SELL position ({_binanceService.CurrentCurrency})");
                    }
                    // Has BUY position with same currency
                    else if (_binanceService.HasPosition && _binanceService.Buy && _binanceService.CurrentCurrency == action.Currency)
                    {
                        //Close BUY Position by market price
                        _monitorSource.Cancel();
                        await _binanceService.ClosePosition();
                        _binanceService.NotifyFinished(action.Currency, true);
                        await _telegramBot.SendCancel(true, action.Currency, timeTaken, entered, calculatedTPSL.Price);

                        //Open SELL Position
                        await TradingView_OnAction(sender, new StrategyAction()
                        {
                            Buy = false,
                            Currency = action.Currency,
                            Take = action.Take,
                            Loss = action.Loss
                        });
                    }
                    // Doesnt have any position
                    else if (!_binanceService.HasPosition)
                    {
                        await _binanceService.RequestSell(action.Currency, calculatedTPSL.Take, calculatedTPSL.Loss);

                        _monitorSource = new CancellationTokenSource();
                        Task monitor = _binanceService.StartMonitorTPSL(action.Currency, action.Buy, calculatedTPSL.Take, calculatedTPSL.Loss, _monitorSource.Token);

                        await _telegramBot.SendShort(action.Currency, calculatedTPSL.Price, calculatedTPSL.Take, calculatedTPSL.Loss);
                    }
                    else if(_binanceService.HasPosition)
                    {
                        string buyString = _binanceService.Buy ? "Buy" : "Sell";
                        _logger.LogWarning($"Can not execute action: Bot already has position ({_binanceService.CurrentCurrency} | {buyString})");
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
            if (!_binanceService.HasPosition)
            {
                _logger.LogWarning($"Can not stop order: Bot doesnt have open position");
                return;
            }
            if(_binanceService.Buy != stop.Buy)
            {
                string name = stop.Buy ? "Buy" : "Sell";
                _logger.LogWarning($"Can not stop order: Bot doesnt have '{name}' position");
                return;
            }
            if(_binanceService.CurrentCurrency != stop.Currency)
            {
                _logger.LogWarning($"Can not stop order: Different currency ({_binanceService.CurrentCurrency} and {stop.Currency})");
                return;
            }
            decimal exitPrice = await _binanceService.GetAvgPrice(stop.Currency);
            decimal enterPrice = _binanceService.Enter;
            decimal priceDifference = stop.Buy ? (exitPrice - enterPrice) : (enterPrice - exitPrice);
            decimal percentageDifference = (priceDifference / enterPrice) * 100;
            bool isTp = percentageDifference > 0.0m;

            TimeSpan timeTaken = DateTime.Now - _binanceService.OrderStarted;

            if (isTp)
            {
                await _telegramBot.SendTP(_binanceService.Buy, _binanceService.CurrentCurrency, timeTaken, enterPrice, exitPrice);
            }
            else
            {
                await _telegramBot.SendSL(_binanceService.Buy, _binanceService.CurrentCurrency, timeTaken, enterPrice, exitPrice);
            }

            // Notify binance service
            _binanceService.NotifyFinished(stop.Currency, stop.Buy);
        }

        public async Task Start()
        {
            _logger.LogInformation("WELLSAIK ALERTS");
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
