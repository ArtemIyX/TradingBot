using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingBot.Data;

namespace TradingBot.Services
{
    public struct StrategyAction
    {
        public bool Buy { get; set; }
        public BinanceCurrency Currency { get; set; }
        public decimal Take { get; set; }
        public decimal Loss { get; set; }
    }

    public struct StrategyStop
    {
        public bool Buy { get; set; }
        public BinanceCurrency Currency { get; set; }
    }

    public delegate Task StrategyActionDelegate(object? sender, StrategyAction action);
    public delegate Task StrategyStopDelegate(object? sender, StrategyStop stop);

    internal class TradingViewService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<WebhookService> _logger;

        public event StrategyActionDelegate OnAction;
        public event StrategyStopDelegate OnStop;

        public TradingViewService(IConfiguration Config,
                         ILogger<WebhookService> Logger)
        {
            _config = Config;
            _logger = Logger;
        }

        public void ProcessRequest(string json)
        {
            _logger.LogInformation($"Processing request: {json}");
            try
            {
                TradingViewRequest request = JsonConvert.DeserializeObject<TradingViewRequest>(json) ?? throw new Exception("Can not parse request");
                BinanceCurrency currecny = request.Currency.ToBinanceCurrency();
               
                if (request.Action.Contains("BUY_") || request.Action.Contains("SELL_"))
                {
                    bool buy = request.Action.Contains("BUY_");
                    string[] substrings = request.Action.Split('_');
                    CultureInfo culture = CultureInfo.GetCultureInfo("en-US");
                    decimal takePer = Decimal.Parse(substrings[1], NumberStyles.Any, culture);
                    decimal stopPer = Decimal.Parse(substrings[2], NumberStyles.Any, culture);
                    OnAction?.Invoke(this, new StrategyAction()
                    {
                        Buy = buy,
                        Currency = currecny,
                        Take = takePer,
                        Loss = stopPer,
                    });
                }
                else if(request.Action.Contains("L_TPSL") || request.Action.Contains("S_TPSL"))
                {
                    bool buy = request.Action.Contains("L_TPSL");
                    OnStop.Invoke(this, new StrategyStop()
                    {
                        Buy = buy,
                        Currency = currecny
                    });
                }
                else
                {
                    throw new Exception($"Unknown Request.Action ({request.Action})");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
