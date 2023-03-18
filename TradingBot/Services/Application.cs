using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private readonly CancellationToken cancellationToken;

        public Application(
            ILogger<Application> logger, 
            IConfiguration config,
            WebhookService webhookService,
            TelegramService telegramBot, 
            BinanceService binanceService,
            TradingViewService tradingViewService)
        {
            _logger = logger;
            _config = config;
            _webhookService = webhookService;
            _telegramBot = telegramBot;
            _binanceService = binanceService;
            _tradingViewService = tradingViewService;
            _tradingViewService.OnAction += TradingView_OnAction;
            _tradingViewService.OnStop += TradingView_OnStop;
        }

        private void TradingView_OnAction(object? sender, StrategyAction action)
        {
            
        }

        private void TradingView_OnStop(object? sender, StrategyStop action)
        {
            
        }

        public async Task Start()
        {
            _logger.LogInformation("WELLSAIK ALERTS");
            _webhookService.WebhookReceived += WebhookReceived;
            _webhookService.StartListeningAsync();
            _telegramBot.Start();
            _binanceService.TryBuy(BinanceCurrency.BTCUSDT, 0.5m);
            await Task.Delay(-1);
        }

        private void WebhookReceived(object? sender, string e)
        {
            _logger.LogInformation($"Webhook received: {e}");

        }

        public void Finish()
        {
            _webhookService.StopListeting();
        }
    }
}
