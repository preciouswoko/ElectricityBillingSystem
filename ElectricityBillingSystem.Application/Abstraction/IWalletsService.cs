using ElectricityBillingSystem.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Application.Abstraction
{
    public interface IWalletsService
    {
        Task<Wallet> GetWalletByIdAsync(Guid id);
        Task<bool> AddFundsAsync(Guid id, decimal amount);
    }
}
