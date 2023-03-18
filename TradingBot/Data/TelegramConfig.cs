using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data
{
    [Serializable]
    internal struct TelegramConfig
    {
        public string Key { get; set; }
        public long ChatId { get; set; }
        public TelegramIcons Icons { get; set; }
    }
    internal struct TelegramIcons
    {
        public string Long { get; set; }
        public string Short { get; set; }
        public string TP { get; set; }
        public string SL { get; set; }
    }
}
