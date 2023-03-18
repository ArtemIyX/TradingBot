using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TradingBot.Data;

namespace TradingBot.Services;

internal class TelegramService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookService> _logger;
    private TelegramBotClient _bot;
    private TelegramConfig _telegramConfig;
  /*  private ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("Option 1"), new KeyboardButton("Option 2") },
                    new[] { new KeyboardButton("Option 3"), new KeyboardButton("Option 4") },
                });
*/

    public TelegramService(IConfiguration Config,
                          ILogger<WebhookService> Logger)
    {
        _config = Config;
        _logger = Logger;
        _telegramConfig = _config.GetSection("Telegram").Get<TelegramConfig>();
    }
    public async Task Start()
    {
        _bot = new TelegramBotClient(_telegramConfig.Key);
        _bot.StartReceiving(Update, Error);
        _logger.LogInformation("Telegram bot started");
        await SendMessageAsync(_telegramConfig.ChatId, "Hello");
    }

    private async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        Message? message = update.Message;
        if (message?.Text != null)
            _logger.LogInformation("Telegam message: " + message.Text);
    }


    private async Task SendMessageAsync(long chatId, string message)
    {
        await _bot.SendTextMessageAsync(new ChatId(chatId), message);
    }

    private Task Error(ITelegramBotClient botClient, Exception ex, CancellationToken token)
    {
        _logger.LogError("Telegram: " + ex.Message);
        return Task.CompletedTask;
    }
}
