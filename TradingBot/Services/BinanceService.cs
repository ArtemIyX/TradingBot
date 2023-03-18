using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TradingBot.Data;

namespace TradingBot.Services;

public enum BinanceCurrency
{
    BTCUSDT,
    ETHUSDT,
    OPUSDT,
    KAVAUSDT,
    AVAXUSDT,
    FLOWUSDT,
    FTMUSDT,
    RVNUSDT,
    INJUSDT,
    SOLUSDT,
    CFXUSDT,
    BNXUSDT,
    HOTUSDT,
    XMRUSDT,
    LQTYUSDT,
    TRUUSDT,
    None
}

public static class BinanceServiceExtensions
{
    public static BinanceCurrency ToBinanceCurrency(this string input)
    {
        BinanceCurrency currency;
        if (Enum.TryParse(input, out currency))
        {
            return currency;
        }
        else
        {
            return BinanceCurrency.None;
        }
    }
}

internal class BinanceService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookService> _logger;

    public BinanceCurrency CurrentCurrency { get; private set; }
    public bool HasPosition { get; private set; }

    public BinanceService(IConfiguration Config,
                     ILogger<WebhookService> Logger)
    {
        CurrentCurrency = BinanceCurrency.None;
        HasPosition = false;
        _config = Config;
        _logger = Logger;
    }

}


