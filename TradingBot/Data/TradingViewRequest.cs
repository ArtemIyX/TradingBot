using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data
{
    [Serializable]
    public class TradingViewRequest
    {
        public TradingViewRequest()
        {
            
        }
        public TradingViewRequest(string? action, string? currency, string? key)
        {
            Action = action;
            Currency = currency;
            Key = key;
        }

        public string? Action { get; set; }
        public string? Currency { get; set; }
        public string? Key { get; set; }
    }

    [Serializable]
    public class TradingViewOpenData
    {
        public decimal TakePercent { get; set; }
        public decimal LossPercent { get;set; }
    }
}

