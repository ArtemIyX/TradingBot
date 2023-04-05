using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data.Config
{
    [Serializable]
    internal struct TelegramConfig
    {
        public string Key { get; set; }
        public long ChatId { get; set; }
        public string[] Emoji { get; set; }
    }
}
