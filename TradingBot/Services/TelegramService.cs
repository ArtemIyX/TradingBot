using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
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
    }

    private async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        Message? message = update.Message;
        if (message?.Text != null)
            _logger.LogInformation("Telegam message: " + message.Text);
    }
    
    private string MakeOpenText(bool buy, BinanceCurrency currency, decimal price, decimal takeProfit, decimal stopLoss)
    {
        string position = buy ? "Long" : "Short";
        return $"{currency.ToString().ToUpper()} {position}\nВход: {price}\nТейк: {takeProfit}\nСтоп: {stopLoss}";
    }

    public async Task SendLong(BinanceCurrency currency, decimal price, decimal takeProfit, decimal stopLoss)
    {
        await _bot.SendPhotoAsync(new ChatId(_telegramConfig.ChatId),
            new InputOnlineFile(new Uri(_telegramConfig.Icons.Long)),
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            caption: MakeOpenText(true, currency, price, takeProfit, stopLoss));
    }

    public async Task SendShort(BinanceCurrency currency, decimal price, decimal takeProfit, decimal stopLoss)
    {
        await _bot.SendPhotoAsync(new ChatId(_telegramConfig.ChatId),
           new InputOnlineFile(new Uri(_telegramConfig.Icons.Short)),
           parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
           caption: MakeOpenText(false, currency, price, takeProfit, stopLoss));
    }

    public async Task SendTP(bool buy, BinanceCurrency currency, decimal enterPrice, decimal exitPrice)
    {
        decimal priceDifference = buy ? (exitPrice - enterPrice) : (enterPrice - exitPrice);
        decimal percentageDifference = (priceDifference / enterPrice) * 100;
        string position = buy ? "Long" : "Short";

        await _bot.SendPhotoAsync(new ChatId(_telegramConfig.ChatId),
          new InputOnlineFile(new Uri(_telegramConfig.Icons.TP)),
          parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
          caption: $"{currency.ToString().ToUpper()} {position} Take Profit\nВход: {enterPrice}\nВыход: {exitPrice}\nПрофит: {percentageDifference}%");
    }

    public async Task SendSL(bool buy, BinanceCurrency currency, decimal enterPrice, decimal exitPrice)
    {
        decimal priceDifference = buy ? (exitPrice - enterPrice) : (enterPrice - exitPrice);
        decimal percentageDifference = (priceDifference / enterPrice) * 100;
        bool success = buy ? (enterPrice < exitPrice) : (enterPrice > exitPrice);
        string position = buy ? "Long" : "Short";

        await _bot.SendPhotoAsync(new ChatId(_telegramConfig.ChatId),
          new InputOnlineFile(new Uri(_telegramConfig.Icons.SL)),
          parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
          caption: $"{currency.ToString().ToUpper()} {position} Stop Loss\nВход: {enterPrice}\nВыход: {exitPrice}\nПотеря: {percentageDifference}%");
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
