using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingDataBaseLib.Data
{
    public class TradingAction
    {
        [Key]
        public int Id { get; set; }

        [NotNull]
        public DateTime ClosedAt { get; set; }

        [NotNull]
        public bool Long { get; set; }

        [AllowNull]
        public string? Currency { get; set; }

        [NotNull]
        public decimal Balance { get; set; }

        [AllowNull]
        public string? Reason { get; set; }
    }
}
