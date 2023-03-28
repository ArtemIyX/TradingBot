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
        public string WebhookUrl { get; set; }
        public string SecretKey { get; set; }
        public bool Cancel { get; set; }
        public bool DefaultTakeProfitEnabled { get; set; }
        public decimal DefaultTakeProfit { get; set; }
        public List<BinancePip> Pips { get; set; }
    }

    internal class BinancePip
    {
        public string Currency { get; set; }
        public decimal PipSize { get; set; }
        public decimal TP { get; set; }
        public decimal SL { get; set; }
    }
}
