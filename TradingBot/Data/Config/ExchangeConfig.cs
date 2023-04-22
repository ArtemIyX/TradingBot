using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data.Config
{
    internal struct ExchangeServiceConfig
    {
        public bool Status { get; set; }
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }

        public bool Fixed { get; set; }
        public decimal OrderSizePercent { get; set; }
        
        public decimal Risk { get; set; }
        public int Leverage { get; set; }
    }


}
