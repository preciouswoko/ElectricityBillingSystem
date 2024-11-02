using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Domain.Events
{
    public class WalletFundedEvent
    {
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public decimal NewBalance { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
