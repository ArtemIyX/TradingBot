using TradingBot.Data;

namespace TradingBot.Services;

public static class BinanceServiceExtensions
{
    public static CryptoCurrency ToBinanceCurrency(this string? input)
    {
        if (Enum.TryParse(input, out CryptoCurrency currency))
        {
            return currency;
        }

        return CryptoCurrency.None;
    }

    public static string ConvertKlineIntervalToString(Binance.Net.Enums.KlineInterval interval) =>
        interval switch
        {
            Binance.Net.Enums.KlineInterval.OneSecond => "1s",
            Binance.Net.Enums.KlineInterval.OneMinute => "1m",
            Binance.Net.Enums.KlineInterval.ThreeMinutes => "3m",
            Binance.Net.Enums.KlineInterval.FiveMinutes => "5m",
            Binance.Net.Enums.KlineInterval.FifteenMinutes => "15m",
            Binance.Net.Enums.KlineInterval.ThirtyMinutes => "30m",
            Binance.Net.Enums.KlineInterval.OneHour => "1h",
            Binance.Net.Enums.KlineInterval.TwoHour => "2h",
            Binance.Net.Enums.KlineInterval.FourHour => "4h",
            Binance.Net.Enums.KlineInterval.SixHour => "6h",
            Binance.Net.Enums.KlineInterval.EightHour => "8h",
            Binance.Net.Enums.KlineInterval.TwelveHour => "12h",
            Binance.Net.Enums.KlineInterval.OneDay => "1d",
            Binance.Net.Enums.KlineInterval.ThreeDay => "3d",
            Binance.Net.Enums.KlineInterval.OneWeek => "1w",
            Binance.Net.Enums.KlineInterval.OneMonth => "1M",
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
        };
    
}