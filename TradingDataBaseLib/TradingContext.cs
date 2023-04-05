using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingDataBaseLib.Data;

namespace TradingDataBaseLib
{
    public class TradingContext : DbContext
    {
        public DbSet<TradingAction> TradingActions { get; set; }


        public TradingContext(DbContextOptions options) : base(options) 
        { 

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
