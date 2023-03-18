using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TradingBot.Data;

namespace TradingBot.Services;

class WebhookService
{
    public event EventHandler<string> WebhookReceived;

    private readonly IConfiguration _config;
    private readonly ILogger<WebhookService> _logger;
    private HttpListener _httpListener;

    public WebhookService(IConfiguration Config,
                          ILogger<WebhookService> Logger)
    {
        _config = Config;
        _logger = Logger;
    }

    public async Task StartListeningAsync()
    {
        // Create an HTTP listener object and start it
        _httpListener = new HttpListener();
        WebhookConfig? webhookConfig = _config.GetSection("Webhook").Get<WebhookConfig>();
        _logger.LogInformation($"Listening for webhooks on {webhookConfig.Url}...");
        _httpListener.Prefixes.Add(webhookConfig.Url);
        _httpListener.Start();

        while (_httpListener.IsListening)
        {
            try
            {
                // Wait for an incoming request
                HttpListenerContext context = await _httpListener.GetContextAsync();

                // Read the request body
                byte[] body = new byte[context.Request.ContentLength64];
                context.Request.InputStream.Read(body, 0, body.Length);
                string requestBody = Encoding.UTF8.GetString(body);

                // Raise the WebhookReceived event with the received data
                WebhookReceived?.Invoke(this, requestBody);

                // Send a response back to the client
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.Close();
            }
            catch(Exception)
            {

            }
        }
    }
    public void StopListeting()
    {
        _httpListener.Stop();
        _httpListener.Close();
    }
}
