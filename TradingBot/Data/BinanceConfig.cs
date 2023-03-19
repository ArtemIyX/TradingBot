using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data
{
    internal struct BinanceConfig
    {
        public bool Status { get; set; }
        public decimal TakeProfitPercent { get; set; }
        public decimal StopLossPercent { get; set; }
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
        public bool TestNet { get; set; }
        public decimal OrderSizePercent { get; set; }
        public int Leverage { get; set; }
    }
}
