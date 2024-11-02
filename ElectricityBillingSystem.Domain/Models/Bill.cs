using ElectricityBillingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Domain.Models
{
    public class Bill
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public BillStatus Status { get; set; }
        public string MeterNumber { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }

        public string ValidationReference { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Token { get; set; }
    }
}
