using TradingBot.Data;

namespace TradingBot.Services;

public static class ServiceExtensions
{
    public static CryptoCurrency ToCrtypoCurrency(this string? input)
    {
        if (Enum.TryParse(input, out CryptoCurrency currency))
        {
            return currency;
        }
        return CryptoCurrency.None;
    }
}