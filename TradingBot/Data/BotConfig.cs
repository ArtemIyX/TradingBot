using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data
{
    [Serializable]
    internal struct BotConfig
    {
        public string WebhookUrl { get; set; }
        public string SecretKey { get; set; }
    }
}
