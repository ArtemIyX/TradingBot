using CryptoExchange.Net.Objects;

namespace TradingBot.Data;

public class PlaceOrderResult
{
    public WebCallResult<Bybit.Net.Objects.Models.BybitUsdPerpetualOrder>? WebCallResult { get; set; }
    public decimal Qty { get; set; }
}