using ElectricityBillingSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Application.Abstraction
{
    public interface IElectricityService
    {
        Task<(bool isValid, string customerName)> VerifyMeterAsync(string meterNumber);
        Task<string> CreateBillAsync(BillVerificationRequest request);
        Task<string> ProcessPaymentAsync(string validationRef, Guid walletId);
    }
}
