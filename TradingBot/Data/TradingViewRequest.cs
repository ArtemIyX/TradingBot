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
        public string? Action { get; set; }
        public string? Currency { get; set; }
        public string? Key { get; set; }
        
        public decimal Take { get; set; }
        
        public decimal Stop { get; set; }
    }
    
}

