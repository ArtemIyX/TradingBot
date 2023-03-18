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
using Telegram.Bot.Types.Enums;
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
        var split = currency.ToString().Split("USDT");
        string emoji = buy ? _telegramConfig.Emoji[1] : _telegramConfig.Emoji[0];
        
        return $"{emoji} #{split[0]}/{split[1]} {position}\n" +
            $"{_telegramConfig.Emoji[4]} Enter: {price}\n" +
            $"{_telegramConfig.Emoji[5]} TP: {takeProfit}\n" +
            $"{_telegramConfig.Emoji[6]} SL: {stopLoss}";
    }

    private string MakeTPSLText(bool buy, bool tp, BinanceCurrency currency, decimal enter, decimal exit, decimal percentageDifference)
    {
        string position = buy ? "Long" : "Short";
        string tpString = tp ? "TP" : "SL";
        string smile = percentageDifference < 0 ? _telegramConfig.Emoji[3] : "";
        string profit = percentageDifference > 0 ? "Profit" : "Loss";
        var split = currency.ToString().Split("USDT");
        string emoji = buy ? _telegramConfig.Emoji[1] : _telegramConfig.Emoji[0];

        return $"{emoji} #{split[0]}/{split[1]} {position} {tpString}\n" +
           $"{_telegramConfig.Emoji[4]} Enter: {enter}\n" +
           $"{_telegramConfig.Emoji[6]} Exit: {exit}\n" +
           $"{_telegramConfig.Emoji[2]} {profit}: {percentageDifference}% {smile}";
    }

    public async Task SendLong(BinanceCurrency currency, decimal price, decimal takeProfit, decimal stopLoss)
    {
        await _bot.SendTextMessageAsync(new ChatId(_telegramConfig.ChatId),
             MakeOpenText(true, currency, price, takeProfit, stopLoss));
    }

    public async Task SendShort(BinanceCurrency currency, decimal price, decimal takeProfit, decimal stopLoss)
    {
        await _bot.SendTextMessageAsync(new ChatId(_telegramConfig.ChatId),
             MakeOpenText(false, currency, price, takeProfit, stopLoss));
    }

    public async Task SendTP(bool buy, BinanceCurrency currency, decimal enterPrice, decimal exitPrice)
    {
        decimal priceDifference = buy ? (exitPrice - enterPrice) : (enterPrice - exitPrice);
        decimal percentageDifference = (priceDifference / enterPrice) * 100;

        await _bot.SendTextMessageAsync(new ChatId(_telegramConfig.ChatId),
            MakeTPSLText(buy, true, currency, enterPrice, exitPrice, percentageDifference));
    }

    public async Task SendSL(bool buy, BinanceCurrency currency, decimal enterPrice, decimal exitPrice)
    {
        decimal priceDifference = buy ? (exitPrice - enterPrice) : (enterPrice - exitPrice);
        decimal percentageDifference = (priceDifference / enterPrice) * 100;
        bool success = buy ? (enterPrice < exitPrice) : (enterPrice > exitPrice);
        string position = buy ? "Long" : "Short";

        await _bot.SendTextMessageAsync(new ChatId(_telegramConfig.ChatId),
            MakeTPSLText(buy, false, currency, enterPrice, exitPrice, percentageDifference));
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
