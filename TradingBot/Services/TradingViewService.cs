﻿using Microsoft.Extensions.Configuration;
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

        public event StrategyActionDelegate OnAction;
        public event StrategyStopDelegate OnStop;

        public TradingViewService(IConfiguration Config,
                         ILogger<WebhookService> Logger)
        {
            _config = Config;
            _logger = Logger;
            _binanceConfig = _config.GetSection("Binance").Get<BinanceConfig>();
        }

        public void ProcessRequest(string json)
        {
            _logger.LogInformation($"Processing request: {json}");
            try
            {
                TradingViewRequest request = JsonConvert.DeserializeObject<TradingViewRequest>(json) ?? throw new Exception("Can not parse request");
                BinanceCurrency currecny = request.Currency.ToBinanceCurrency();
               
                if (request.Action == "BUY" || request.Action == "SELL")
                {
                    bool buy = request.Action == "BUY";
                    OnAction?.Invoke(this, new StrategyAction()
                    {
                        Buy = buy,
                        Currency = currecny,
                        Take = _binanceConfig.TakeProfitPercent,
                        Loss = _binanceConfig.StopLossPercent,
                    });
                }
                else if(request.Action == "L_TPSL" || request.Action == "S_TPSL")
                {
                    bool buy = request.Action == "L_TPSL";
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
