using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        public decimal Price { get; set; }
        public decimal Take { get; set; }
        public decimal Loss { get; set; }
    }

    public struct StrategyStop
    {
        public bool Buy { get; set; }
        public decimal Price { get; set; }
    }

    public delegate void StrategyActionDelegate(object? sender, StrategyAction action);
    public delegate void StrategyStopDelegate(object? sender, StrategyStop action);

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
                TradingViewRequest request = JsonConvert.DeserializeObject<TradingViewRequest>(json);
                BinanceCurrency currecny = request.Currency.ToBinanceCurrency();
                if(request.Action == "BUY" || request.Action == "SELL")
                {
                    bool buy = request.Action == "BUY";
                    JObject data = request.Data as JObject ?? throw new NullReferenceException("Request.Data is null");
                    TradingViewOpenData priceData = data.ToObject<TradingViewOpenData>();
                    OnAction?.Invoke(this, new StrategyAction()
                    {
                        Buy = buy,
                        Currency = currecny,
                        Price = request.Price,
                        Take = priceData.Take,
                        Loss = priceData.Loss
                    });
                }
                else if(request.Action == "BUY_TPSL" || request.Action == "SELL_TPSL")
                {
                    bool buy = request.Action == "BUY_TPSL";
                    OnStop.Invoke(this, new StrategyStop()
                    {
                        Buy = buy,
                        Price = request.Price
                    });
                }
                else
                {
                    throw new Exception("Unknown Request.Action");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
