using ElectricityBillingSystem.Application.Abstraction;
using ElectricityBillingSystem.Domain.Models;
using ElectricityBillingSystem.Infrastructure.Data;
using ElectricityBillingSystem.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Application.Concrete
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IDistributedCache _cache;
        private readonly ILogger<UserService> _logger;

        public UserService(AppDbContext context, IConfiguration config, IDistributedCache cache, ILogger<UserService> logger)
        {
            _context = context;
            _config = config;
            _cache = cache;
            _logger = logger;
        }

        public async Task<User?> AuthenticateUserAsync(string email, string password)
        {
            _logger.LogInformation("Authenticating user with email: {Email}", email);
            string hashedPassword = Helper.ComputeSHA256Hash(password);
            // Query the user based on email
            var user = await _context.Users
      .Include(u => u.Wallet) // Include Wallet in the query
      .SingleOrDefaultAsync(u => u.Email == email && u.PasswordHash == hashedPassword);

            if (user == null)
            {
                _logger.LogWarning("Authentication failed for user with email: {Email}", email);
            }
            return user;
        }

        public async Task StoreJwtTokenInCacheAsync(Guid userId, string token)
        {
            var expiryMinutes = double.Parse(_config["Jwt:ExpiryMinutes"]);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiryMinutes)
            };

            await _cache.SetStringAsync(userId.ToString(), token, cacheOptions);
            _logger.LogInformation("JWT token stored in cache for user ID: {UserId} with expiration: {ExpiryMinutes} minutes", userId, expiryMinutes);
        }

        public async Task RemoveJwtTokenFromCacheAsync(string userId)
        {
            await _cache.RemoveAsync(userId);
            _logger.LogInformation("JWT token removed from cache for user ID: {UserId}", userId);
        }

        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("WalletId", user.Wallet?.Id.ToString() ?? string.Empty), // Custom claim for Wallet ID
                new Claim(ClaimTypes.Email, user.Email)
            }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"])),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            _logger.LogInformation("Generated JWT token for user ID: {UserId}", user.Id);
            //// Decode the token to verify claims (optional)
            //var decodedToken = tokenHandler.ReadJwtToken(tokenString);
            //var walletIdClaim = decodedToken.Claims.FirstOrDefault(c => c.Type == "WalletId")?.Value;

            return tokenString;
        }
    }

}
