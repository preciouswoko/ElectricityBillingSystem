using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Domain.Events
{
    public class BillCreatedEvent
    {
        public Guid BillId { get; set; }
        public decimal Amount { get; set; }
        public string MeterNumber { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string ValidationReference { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
