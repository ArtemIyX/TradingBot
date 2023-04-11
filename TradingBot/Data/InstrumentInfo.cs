using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Data
{
    public class InstrumentInfoResult
    {
        public string Category { get; set; }
        public Instrument[] List { get; set; }
        public string NextPageCursor { get; set; }
    }

    public class Instrument
    {
        public string Symbol { get; set; }
        public string ContractType { get; set; }
        public string Status { get; set; }
        public string BaseCoin { get; set; }
        public string QuoteCoin { get; set; }
        public string LaunchTime { get; set; }
        public string DeliveryTime { get; set; }
        public string DeliveryFeeRate { get; set; }
        public LeverageFilter LeverageFilter { get; set; }
        public PriceFilter PriceFilter { get; set; }
        public LotSizeFilter LotSizeFilter { get; set; }
        public bool UnifiedMarginTrade { get; set; }
        public int FundingInterval { get; set; }
        public string SettleCoin { get; set; }
    }

    public class LeverageFilter
    {
        public string MinLeverage { get; set; }
        public string MaxLeverage { get; set; }
        public string LeverageStep { get; set; }
    }

    public class PriceFilter
    {
        public string MinPrice { get; set; }
        public string MaxPrice { get; set; }
        public string TickSize { get; set; }
    }

    public class LotSizeFilter
    {
        public string MaxTradingQty { get; set; }
        public string MinTradingQty { get; set; }
        public string QtyStep { get; set; }
        public string PostOnlyMaxOrderQty { get; set; }
        public string MaxOrderQty { get; set; }
        public string MinOrderQty { get; set; }
    }
}
