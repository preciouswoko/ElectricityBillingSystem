using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Application.DTOs
{
    public class BillVerificationRequest
    {
        public string MeterNumber { get; set; }
        public decimal Amount { get; set; }
    }
}
