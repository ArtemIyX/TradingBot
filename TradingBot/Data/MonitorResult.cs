namespace TradingBot.Data;

public struct MonitorResult
{
    public CryptoCurrency Currency { get; set; }
    public bool Take { get; set; }
    public bool Buy { get; set; }
    public decimal Price { get; set; }
}