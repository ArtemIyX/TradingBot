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
        private readonly CancellationToken cancellationToken;

        public Application(
            ILogger<Application> logger, 
            IConfiguration config,
            WebhookService webhookService,
            TelegramService telegramBot)
        {
            _logger = logger;
            _config = config;
            _webhookService = webhookService;
            _telegramBot = telegramBot;
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
            _logger.LogInformation($"Webhook received: {e}");
        }

        public void Finish()
        {
            _webhookService.StopListeting();
        }
    }
}
