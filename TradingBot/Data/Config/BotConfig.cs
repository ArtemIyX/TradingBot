using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data.Config
{
    [Serializable]
    internal class BotConfig
    {
        public BotConfig()
        {

        }
        public BotConfig(string? webhookUrl, string? secretKey, bool cancel, List<BinancePip>? pips)
        {
            WebhookUrl = webhookUrl;
            SecretKey = secretKey;
            Cancel = cancel;
            Pips = pips;
        }

        public string? WebhookUrl { get; set; }
        public string? SecretKey { get; set; }
        public bool Cancel { get; set; }
        public List<BinancePip>? Pips { get; set; }
    }

    [Serializable]
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
