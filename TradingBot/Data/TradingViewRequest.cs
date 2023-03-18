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
        public decimal Price { get; set; }
        public object? Data { get; set; }   
    }

    [Serializable]
    public class TradingViewOpenData
    {
        public decimal Take { get; set; }
        public decimal Loss { get;set; }
    }
}

