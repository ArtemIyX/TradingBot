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
        private readonly CancellationToken cancellationToken;

        public Application(
            ILogger<Application> logger, 
            IConfiguration config,
            WebhookService webhookService)
        {
            _logger = logger;
            _config = config;
            _webhookService = webhookService;
        }
        public async Task Start()
        {
            _logger.LogInformation("Hello world!");
            _webhookService.WebhookReceived += WebhookReceived;
            _webhookService.StartListeningAsync();
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
