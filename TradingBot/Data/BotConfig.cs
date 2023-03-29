using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data
{
    [Serializable]
    internal class BotConfig
    {
        public BotConfig()
        {
            
        }
        public BotConfig(string? webhookUrl, string? secretKey, bool useBinance, bool cancel, bool defaultTakeProfitEnabled, decimal defaultTakeProfit, List<BinancePip>? pips)
        {
            WebhookUrl = webhookUrl;
            SecretKey = secretKey;
            UseBinance = useBinance;
            Cancel = cancel;
            DefaultTakeProfitEnabled = defaultTakeProfitEnabled;
            DefaultTakeProfit = defaultTakeProfit;
            Pips = pips;
        }

        public string? WebhookUrl { get; set; }
        public string? SecretKey { get; set; }
        public bool UseBinance { get; set; }
        public bool Cancel { get; set; }
        public bool DefaultTakeProfitEnabled { get; set; }
        public decimal DefaultTakeProfit { get; set; }
        public List<BinancePip>? Pips { get; set; }
    }

    public class BinancePip
    {
        public BinancePip()
        {
            
        }
        public BinancePip(string? currency, decimal pipSize, decimal tp, decimal sl)
        {
            Currency = currency;
            PipSize = pipSize;
            Tp = tp;
            Sl = sl;
        }

        public string? Currency { get; set; }
        public decimal PipSize { get; set; }
        public decimal Tp { get; set; }
        public decimal Sl { get; set; }
    }
}
