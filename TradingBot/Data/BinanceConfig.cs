using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data
{
    internal class BinanceConfig
    {
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
        public bool TestNet { get; set; }
        public decimal Percent { get; set; }
    }
}
