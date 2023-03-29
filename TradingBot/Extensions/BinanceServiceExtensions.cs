namespace TradingBot.Services;

public static class BinanceServiceExtensions
{
    public static CryptoCurrency ToBinanceCurrency(this string input)
    {
        CryptoCurrency currency;
        if (Enum.TryParse(input, out currency))
        {
            return currency;
        }
        else
        {
            return CryptoCurrency.None;
        }
    }

    public static string ConvertKlineIntervalToString(Binance.Net.Enums.KlineInterval interval)
    {
        switch (interval)
        {
            case Binance.Net.Enums.KlineInterval.OneSecond:
                return "1s";
            case Binance.Net.Enums.KlineInterval.OneMinute:
                return "1m";
            case Binance.Net.Enums.KlineInterval.ThreeMinutes:
                return "3m";
            case Binance.Net.Enums.KlineInterval.FiveMinutes:
                return "5m";
            case Binance.Net.Enums.KlineInterval.FifteenMinutes:
                return "15m";
            case Binance.Net.Enums.KlineInterval.ThirtyMinutes:
                return "30m";
            case Binance.Net.Enums.KlineInterval.OneHour:
                return "1h";
            case Binance.Net.Enums.KlineInterval.TwoHour:
                return "2h";
            case Binance.Net.Enums.KlineInterval.FourHour:
                return "4h";
            case Binance.Net.Enums.KlineInterval.SixHour:
                return "6h";
            case Binance.Net.Enums.KlineInterval.EightHour:
                return "8h";
            case Binance.Net.Enums.KlineInterval.TwelveHour:
                return "12h";
            case Binance.Net.Enums.KlineInterval.OneDay:
                return "1d";
            case Binance.Net.Enums.KlineInterval.ThreeDay:
                return "3d";
            case Binance.Net.Enums.KlineInterval.OneWeek:
                return "1w";
            case Binance.Net.Enums.KlineInterval.OneMonth:
                return "1M";
            default:
                throw new ArgumentOutOfRangeException(nameof(interval), interval, null);
        }
    }
}