﻿using Microsoft.Extensions.Configuration;
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
using static System.Collections.Specialized.BitVector32;

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
       /* Message? message = update.Message;
        if (message?.Text != null)
            _logger.LogInformation("Telegam message: " + message.Text);*/
    }
    private async Task Error(ITelegramBotClient botClient, Exception ex, CancellationToken token)
    {
        //_logger.LogError("Telegram: " + ex.Message);
    }

    private string MakeOpenText(bool buy, BinanceCurrency currency, decimal price, decimal takeProfit, decimal stopLoss)
    {
        string position = buy ? "Long" : "Short";
        string[] split = currency.ToString().Split("USDT");
        string emoji = buy ? _telegramConfig.Emoji[1] : _telegramConfig.Emoji[0];
        
        return $"{emoji} #{split[0]}/{split[1]} {position}\n" +
            $"{_telegramConfig.Emoji[4]} Enter: {Math.Round(price, 3)}\n" +
            $"{_telegramConfig.Emoji[5]} TP: {Math.Round(takeProfit, 3)}\n" +
            $"{_telegramConfig.Emoji[6]} SL: {Math.Round(stopLoss, 3)}";
    }

    private string MakeTPSLText(bool buy, bool tp, BinanceCurrency currency, TimeSpan timeTaken, decimal enter, decimal exit, decimal percentageDifference)
    {
        string position = buy ? "Long" : "Short";
        string tpString = tp ? "TP" : "SL";
        string badSmile = percentageDifference < 0 ? _telegramConfig.Emoji[3] : "";
        bool profit = percentageDifference > 0;
        string profitStr = profit ? "Profit" : "Loss";
        string firstEmoji = tp ? _telegramConfig.Emoji[9] : _telegramConfig.Emoji[10];
        string secondEmoji = profit ? _telegramConfig.Emoji[8] : _telegramConfig.Emoji[6];
        string[] split = currency.ToString().Split("USDT");
        string directionEmoji = buy ? _telegramConfig.Emoji[1] : _telegramConfig.Emoji[0];


        return $"{firstEmoji}{firstEmoji}{firstEmoji}\n"+ 
           $"{directionEmoji} #{split[0]} /{split[1]} {position} {tpString}\n" +
           $"{_telegramConfig.Emoji[4]} Enter: {Math.Round(enter, 3)}\n" +
           $"{secondEmoji} Exit: {Math.Round(exit, 3)}\n" +
           $"{_telegramConfig.Emoji[2]} {profitStr}: {Math.Round(percentageDifference, 3)}% {badSmile}\n" +
           $"{_telegramConfig.Emoji[7]} Time: {Math.Round(timeTaken.TotalMinutes, 1)}m";
    }

    private string MakeCancelText(bool wasBuy, BinanceCurrency currency, TimeSpan timeTaken, decimal enter, decimal exit, decimal percentageDifference)
    {
        string position = wasBuy ? "Long" : "Short";
        string[] split = currency.ToString().Split("USDT");
        string directionEmoji = wasBuy ? _telegramConfig.Emoji[1] : _telegramConfig.Emoji[0];
        bool profit = percentageDifference > 0;
        string secondEmoji = profit ? _telegramConfig.Emoji[8] : _telegramConfig.Emoji[6];
        string profitStr = profit ? "Profit" : "Loss";
        string badSmile = percentageDifference < 0 ? _telegramConfig.Emoji[3] : "";

        return $"{directionEmoji} #{split[0]} /{split[1]} {position} Canceled {_telegramConfig.Emoji[10]}\n" +
             $"{_telegramConfig.Emoji[4]} Enter: {Math.Round(enter, 3)}\n" +
            $"{secondEmoji} Exit: {Math.Round(exit, 3)}\n" +
              $"{_telegramConfig.Emoji[2]} {profitStr}: {Math.Round(percentageDifference, 3)}% {badSmile}\n" +
              $"{_telegramConfig.Emoji[7]} Time: {Math.Round(timeTaken.TotalMinutes, 1)}m";
    }

    public async Task SendLong(BinanceCurrency currency, decimal price, decimal takeProfit, decimal stopLoss)
    {
        _logger.LogInformation("Sending Long to telegram");
        await _bot.SendTextMessageAsync(new ChatId(_telegramConfig.ChatId),
             MakeOpenText(true, currency, price, takeProfit, stopLoss));
    }

    public async Task SendShort(BinanceCurrency currency, decimal price, decimal takeProfit, decimal stopLoss)
    {
        _logger.LogInformation("Sending Short to telegram");
        await _bot.SendTextMessageAsync(new ChatId(_telegramConfig.ChatId),
             MakeOpenText(false, currency, price, takeProfit, stopLoss));
    }

    public async Task SendTP(bool buy, BinanceCurrency currency, TimeSpan timeTaken, decimal enterPrice, decimal exitPrice)
    {
        _logger.LogInformation("Sending TP to telegram");
        decimal percentageDifference = GetPercentageDiff(buy, enterPrice, exitPrice);

        await _bot.SendTextMessageAsync(new ChatId(_telegramConfig.ChatId),
            MakeTPSLText(buy, true, currency, timeTaken, enterPrice, exitPrice, percentageDifference));
    }

    public async Task SendSL(bool buy, BinanceCurrency currency, TimeSpan timeTaken, decimal enterPrice, decimal exitPrice)
    {
        _logger.LogInformation("Sending SL to telegram");
        decimal percentageDifference = GetPercentageDiff(buy, enterPrice, exitPrice);

        await _bot.SendTextMessageAsync(new ChatId(_telegramConfig.ChatId),
            MakeTPSLText(buy, false, currency, timeTaken, enterPrice, exitPrice, percentageDifference));
    }

    public async Task SendCancel(bool wasBuy, BinanceCurrency currency, TimeSpan timeTaken, decimal enterPrice, decimal exitPrice)
    {
        _logger.LogInformation("Sending Cancel to telegram");
        decimal percentageDifference = GetPercentageDiff(wasBuy, enterPrice, exitPrice);
        await _bot.SendTextMessageAsync(new ChatId(_telegramConfig.ChatId),
            MakeCancelText(wasBuy, currency, timeTaken, enterPrice, exitPrice, percentageDifference));
    }

    private decimal GetPercentageDiff(bool buy, decimal enterPrice, decimal exitPrice)
    {
        decimal priceDifference = buy ? (exitPrice - enterPrice) : (enterPrice - exitPrice);
        decimal percentageDifference = (priceDifference / enterPrice) * 100;
        return percentageDifference;
    }

    private async Task SendMessageAsync(long chatId, string message)
    {
        await _bot.SendTextMessageAsync(new ChatId(chatId), message);
    }


}
