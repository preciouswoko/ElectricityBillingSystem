using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Domain.Events
{
    public class PaymentCompletedEvent
    {
        public Guid BillId { get; set; }
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public string Token { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
