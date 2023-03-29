namespace TradingBot.Services;

public struct TakeProfitStopLossResult
{
    public decimal Price { get; set; }
    public decimal Take { get; set; }
    public decimal Loss { get; set; }
    public bool Success { get; set; }
}