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

        public string? WebhookUrl { get; set; }
        public string? SecretKey { get; set; }

        public int RecentLen { get; set; }
        public string RecentTF { get; set; }

        public bool Cancel { get; set; }
        
        public bool PutTakeProfit { get; set; }
        public decimal TakeProfitRatio { get; set; }
        
        public decimal Risk { get; set; }
    }
    
}
