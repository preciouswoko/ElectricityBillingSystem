using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Infrastructure.IServices
{

    public interface IElectricityProvider
    {
        Task<(bool isValid, string customerName)> ValidateMeterAsync(string meterNumber);
        Task<string> GenerateTokenAsync(string meterNumber, decimal amount);
        Task<(bool isValid, string providerName)> ValidateProviderAsync(string meterNumber);
    }
}
