using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data
{
    [Serializable]
    internal class TradingViewRequest
    {
        public string Action { get; set; }
        public string Currency { get; set; }
        public string Key { get; set; }
    }

    [Serializable]
    public class TradingViewOpenData
    {
        public decimal TakePercent { get; set; }
        public decimal LossPercent { get;set; }
    }
}

