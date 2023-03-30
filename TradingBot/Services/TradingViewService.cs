using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TradingBot.Data;

namespace TradingBot.Services
{
    public class StrategyAction
    {
        public bool Buy { get; set; }
        public CryptoCurrency Currency { get; set; }
        public decimal Take { get; set; }
    }

    public class StrategyAdvancedAction : StrategyAction
    {
        public decimal Loss { get; set; }
        public decimal PipSize { get; set; }
    }

    public struct StrategyStop
    {
        public bool Buy { get; set; }
        public CryptoCurrency Currency { get; set; }
    }

    public delegate Task StrategyActionDelegate(object? sender, StrategyAction action);

    public delegate Task StrategyStopDelegate(object? sender, StrategyStop stop);

    internal class TradingViewService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<WebhookService> _logger;
        private readonly ExchangeServiceConfig _exchangeServiceConfig;
        private readonly BotConfig _botConfig;


        public event StrategyActionDelegate OnAction;
        public event StrategyStopDelegate OnStop;

        private bool _exucuting = false;

        public TradingViewService(IConfiguration config,
            ILogger<WebhookService> logger)
        {
            _config = config;
            _logger = logger;
            _exchangeServiceConfig = _config.GetSection("ExchangeService").Get<ExchangeServiceConfig>();
            _botConfig = _config.GetSection("Bot").Get<BotConfig>() ?? throw new InvalidOperationException();
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
                _logger.LogError("Can not process request, already have pending reqeust");
                return;
            }

            try
            {
                TradingViewRequest request = JsonConvert.DeserializeObject<TradingViewRequest>(json) ??
                                             throw new Exception("Can not parse request");
                CryptoCurrency currency = request.Currency.ToCrtypoCurrency();

                if (request.Key != _botConfig.SecretKey)
                {
                    throw new Exception("Invalid secret key");
                }

                if (request.Action == "BUY" || request.Action == "SELL")
                {
                    bool buy = request.Action == "BUY";
                    _exucuting = true;
                    // Pip tp/sl

                    BinancePip? needPip = _botConfig.Pips.Find(x => x.Currency == request.Currency.ToString());
                    // if we dont have pip
                    if (needPip == null)
                    {
                        throw new Exception($"Pip profit enabled, but can not find {currency} in settings");
                    }

                    OnAction?.Invoke(this, new StrategyAdvancedAction()
                    {
                        Buy = buy,
                        Currency = currency,
                        Take = needPip.Tp,
                        Loss = needPip.Sl,
                        PipSize = needPip.PipSize
                    });
                }
                else
                {
                    throw new Exception($"Unknown Request.Action ({request.Action})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _exucuting = false;
            }
        }
    }
}