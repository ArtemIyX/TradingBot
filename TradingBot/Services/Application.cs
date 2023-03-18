using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingBot.Services
{
    internal class Application
    {
        private readonly ILogger<Application> _logger;
        private readonly IConfiguration _config;
        public Application(ILogger<Application> logger, IConfiguration config = null)
        {
            _logger = logger;
            _config = config;
        }
        public void Start()
        {
            _logger.LogInformation("Hello world!");
        }
    }
}
