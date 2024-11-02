using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ElectricityBillingSystem.Infrastructure.IServices;
using Microsoft.AspNetCore.Http;

namespace ElectricityBillingSystem.Infrastructure.ElectricityProviders
{

    public class ProviderA : IElectricityProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProviderA(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public Task<(bool isValid, string providerName)> ValidateProviderAsync(string meterNumber)
        {
            // Simulate validation
            var isValid = meterNumber.StartsWith("A");
            return Task.FromResult((isValid, isValid ? "ProviderA" : string.Empty));
        }
        public Task<(bool isValid, string customerName)> ValidateMeterAsync(string meterNumber)
        {
            // Simulate validation
            var isValid = meterNumber.StartsWith("A");
            var customerName = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;

            return Task.FromResult((isValid, isValid ? customerName.ToString() : string.Empty));
        }

        public Task<string> GenerateTokenAsync(string meterNumber, decimal amount)
        {
            // Simulate token generation
            var token = $"A-{Guid.NewGuid()}-{amount}";
            return Task.FromResult(token);
        }
    }
}
