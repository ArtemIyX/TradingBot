using CryptoExchange.Net.Requests;
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
        private readonly BinanceConfig _binanceConfig;
        private readonly BotConfig _botConfig;


        public event StrategyActionDelegate OnAction;
        public event StrategyStopDelegate OnStop;

        private bool _exucuting = false;

        public TradingViewService(IConfiguration Config,
                         ILogger<WebhookService> Logger)
        {
            _config = Config;
            _logger = Logger;
            _binanceConfig = _config.GetSection("Binance").Get<BinanceConfig>();
            _botConfig = _config.GetSection("Bot").Get<BotConfig>();
        }


        public void NotifyFinish()
        {
            _exucuting = false;
        }

        public void ProcessRequest(string json)
        {
            _logger.LogInformation($"Request: {json}");
            if (_exucuting)
            {
                _logger.LogError("Can not process request, already have open reqeust");
                return;
            }
            try
            {
                TradingViewRequest request = JsonConvert.DeserializeObject<TradingViewRequest>(json) ?? throw new Exception("Can not parse request");
                BinanceCurrency currecny = request.Currency.ToBinanceCurrency();
                
                if(request.Key != _botConfig.SecretKey)
                {
                    throw new Exception("Invalid secret key");
                }

                if (request.Action == "BUY" || request.Action == "SELL")
                {
                    bool buy = request.Action == "BUY";
                    _exucuting = true;
                    OnAction?.Invoke(this, new StrategyAction()
                    {
                        Buy = buy,
                        Currency = currecny,
                        Take = _binanceConfig.TakeProfit,
                    });
                }
                /*else if(request.Action == "L_TPSL" || request.Action == "S_TPSL")
                {
                    bool buy = request.Action == "L_TPSL";
                    _exucuting = true;
                    OnStop.Invoke(this, new StrategyStop()
                    {
                        Buy = buy,
                        Currency = currecny
                    });
                }*/
                else
                {
                    throw new Exception($"Unknown Request.Action ({request.Action})");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                _exucuting = false;
            }
        }
    }
}
