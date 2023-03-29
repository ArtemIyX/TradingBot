using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data
{
    internal struct ExchangeServiceConfig
    {
        public bool Status { get; set; }
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }

        public decimal OrderSizePercent { get; set; }
        public int Leverage { get; set; }
    }


}
