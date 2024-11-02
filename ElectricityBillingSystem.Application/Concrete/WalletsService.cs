using ElectricityBillingSystem.Application.Abstraction;
using ElectricityBillingSystem.Domain.Events;
using ElectricityBillingSystem.Domain.Models;
using ElectricityBillingSystem.Infrastructure.Data;
using ElectricityBillingSystem.Infrastructure.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Application.Concrete
{
    public class WalletsService : IWalletsService
    {
        private readonly AppDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ISmsService _smsService;
        private readonly ILogger<WalletsService> _logger;

        public WalletsService(
            AppDbContext context,
            IEventPublisher eventPublisher,
            ISmsService smsService,
            ILogger<WalletsService> logger)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task<Wallet> GetWalletByIdAsync(Guid id)
        {
            _logger.LogInformation("Retrieving wallet with ID {WalletId}", id);
            //var wallet = await _context.Wallets.FindAsync(id);
            var wallet = await _context.Wallets
    .Include(w => w.User)
    .FirstOrDefaultAsync(w => w.Id == id);
            if (wallet == null)
            {
                _logger.LogWarning("Wallet with ID {WalletId} not found", id);
            }
            else
            {
                _logger.LogInformation("Wallet with ID {WalletId} retrieved successfully", id);
            }

            return wallet;
        }

        public async Task<bool> AddFundsAsync(Guid id, decimal amount)
        {
            _logger.LogInformation("Adding {Amount} to wallet with ID {WalletId}", amount, id);
            var wallet = await GetWalletByIdAsync(id);

            if (wallet == null)
            {
                _logger.LogWarning("Failed to add funds: Wallet with ID {WalletId} does not exist", id);
                return false;
            }

            wallet.Balance += amount;
            wallet.LastUpdated = DateTime.UtcNow;

            _logger.LogInformation("Wallet balance updated. New balance: {Balance}", wallet.Balance);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Changes saved to the database for wallet ID {WalletId}", id);

            // Publish event
            var walletFundedEvent = new WalletFundedEvent
            {
                WalletId = wallet.Id,
                Amount = amount,
                NewBalance = wallet.Balance,
                Timestamp = DateTime.UtcNow
            };

            await _eventPublisher.PublishAsync("wallet_funded", walletFundedEvent);
            _logger.LogInformation("Published wallet funded event for wallet ID {WalletId}", id);

            // Check if balance is below threshold
            const decimal LOW_BALANCE_THRESHOLD = 1000m;
            if (wallet.Balance < LOW_BALANCE_THRESHOLD)
            {
                _logger.LogWarning("Wallet balance for ID {WalletId} is below threshold: {Balance}", id, wallet.Balance);

                await _smsService.SendSmsAsync(
                    wallet.User.PhoneNumber,
                    $"Low wallet balance alert! Current balance: {wallet.Balance:C}"
                );
                _logger.LogInformation("Low balance SMS alert sent for wallet ID {WalletId}", id);
            }

            _logger.LogInformation("Funds added successfully to wallet ID {WalletId}", id);
            return true;
        }
    }


}
