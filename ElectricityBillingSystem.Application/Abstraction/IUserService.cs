using ElectricityBillingSystem.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Application.Abstraction
{
    public interface IUserService
    {
        Task<User?> AuthenticateUserAsync(string email, string password);
        string GenerateJwtToken(User user);
        Task RemoveJwtTokenFromCacheAsync(string userId);
        Task StoreJwtTokenInCacheAsync(Guid userId, string token);
    }
}
