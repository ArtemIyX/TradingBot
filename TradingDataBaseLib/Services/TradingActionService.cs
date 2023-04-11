using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TradingDataBaseLib.Data;

namespace TradingDataBaseLib.Services
{
    public class TradingActionService
    {
        private readonly TradingContext _dbContext;
        private readonly ILogger<TradingActionService> _logger;

        public TradingActionService(TradingContext dbContext, ILogger<TradingActionService> logger) 
        {
            this._dbContext = dbContext;
            this._logger = logger;
        }
        public async Task EnsureCreatedDB()
        {
            await _dbContext.Database.EnsureCreatedAsync();
        }

        public async Task AddTradingAction(bool buy, string currency, decimal balance, string reason)
        {
            TradingAction action = new TradingAction()
            {
                Long = buy,
                ClosedAt = DateTime.Now,
                Currency = currency,
                Balance = balance,
                Reason = reason
            };
            _logger.LogInformation($"Adding TradingAction to database: {JsonSerializer.Serialize(action)}");
            await _dbContext.TradingActions.AddAsync(action);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<TradingAction>> GetAll()
        {
            _logger.LogInformation("Getting all trading action from database");
            return await _dbContext.TradingActions.ToListAsync();
        }

        public async Task<TradingAction> GetLast()
        { 
            _logger.LogInformation("Getting last trading action from database");
            return await _dbContext.TradingActions.LastAsync();
        }

        public async Task<IEnumerable<TradingAction>> GetTop(int n)
        {
            _logger.LogInformation($"Getting last {n} from database");
            var action = await _dbContext.TradingActions
                .OrderBy(x => x.ClosedAt)
                .Take(n)
                .ToListAsync();
            return action;
        }

        public async Task<IEnumerable<TradingAction>> GetMonthly()
        {
            var currentDate = DateTime.Today;
            var oneMonthAgo = currentDate.AddMonths(-1);
            var entitiesWithinOneMonth = await _dbContext.TradingActions
               .Where(e => (currentDate - e.ClosedAt).Days <= 30)
               .ToListAsync();
            return entitiesWithinOneMonth;
        }
    }
}
