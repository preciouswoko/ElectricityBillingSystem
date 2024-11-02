using ElectricityBillingSystem.Application.Abstraction;
using ElectricityBillingSystem.Application.DTOs;
using ElectricityBillingSystem.Domain.Enums;
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

    public class ElectricityService : IElectricityService
    {
        private readonly AppDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ISmsService _smsService;
        private readonly IEnumerable<IElectricityProvider> _providers;
        private readonly ILogger<ElectricityService> _logger;

        public ElectricityService(
            AppDbContext context,
            IEventPublisher eventPublisher,
            ISmsService smsService,
            IEnumerable<IElectricityProvider> providers,
            ILogger<ElectricityService> logger)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _smsService = smsService;
            _providers = providers;
            _logger = logger;
        }

        public async Task<(bool isValid, string customerName)> VerifyMeterAsync(string meterNumber)
        {
            _logger.LogInformation("Verifying meter number {MeterNumber}", meterNumber);

            var provider = _providers.FirstOrDefault(p =>
                Task.Run(async () => (await p.ValidateProviderAsync(meterNumber)).isValid).Result);

            if (provider == null)
            {
                _logger.LogWarning("No provider found for meter number {MeterNumber}", meterNumber);
                return (false, null);
            }
            _logger.LogInformation("Meter number {MeterNumber} verified successfully with Provider name {provider}", meterNumber, provider);
            var (_, customerName) = await provider.ValidateMeterAsync(meterNumber);
            _logger.LogInformation("Meter number {MeterNumber} verified successfully with customer name {CustomerName}", meterNumber, customerName);

            return (true, customerName);
        }

        public async Task<string> CreateBillAsync(BillVerificationRequest request)
        {
            _logger.LogInformation("Creating bill for meter number {MeterNumber} with amount {Amount}", request.MeterNumber, request.Amount);

            var (isValid, customerName) = await VerifyMeterAsync(request.MeterNumber);
            if (!isValid)
            {
                _logger.LogWarning("Invalid meter number {MeterNumber} for bill creation", request.MeterNumber);
                throw new ArgumentException("Invalid meter number");
            }

            var bill = new Bill
            {
                Id = Guid.NewGuid(),
                Amount = request.Amount,
                Status = BillStatus.Pending,
                MeterNumber = request.MeterNumber,
                CustomerName = customerName,
                ValidationReference = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow
            };

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bill created with ID {BillId} and validation reference {ValidationReference}", bill.Id, bill.ValidationReference);

            await _eventPublisher.PublishAsync("bill_created", new BillCreatedEvent
            {
                BillId = bill.Id,
                Amount = bill.Amount,
                MeterNumber = bill.MeterNumber,
                CustomerName = bill.CustomerName,
                ValidationReference = bill.ValidationReference,
                Timestamp = DateTime.UtcNow
            });

            await _smsService.SendSmsAsync(
                "dummy-phone",
                $"New bill created for {bill.Amount:C} with reference {bill.ValidationReference}"
            );

            return bill.ValidationReference;
        }

        public async Task<string> ProcessPaymentAsync(string validationRef, Guid walletId)
        {
            _logger.LogInformation("Processing payment for bill with validation reference {ValidationRef}", validationRef);

            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.ValidationReference == validationRef);

            if (bill == null)
            {
                _logger.LogWarning("Bill with validation reference {ValidationRef} not found", validationRef);
                throw new ArgumentException("Bill not found");
            }

            if (bill.Status == BillStatus.Paid)
            {
                _logger.LogWarning("Bill with validation reference {ValidationRef} is already paid", validationRef);
                throw new InvalidOperationException("Bill already paid");
            }

            var wallet = await _context.Wallets.FindAsync(walletId);
            if (wallet == null)
            {
                _logger.LogWarning("Wallet with ID {WalletId} not found", walletId);
                throw new ArgumentException("Wallet not found");
            }

            if (wallet.Balance < bill.Amount)
            {
                _logger.LogWarning("Insufficient funds in wallet {WalletId} for bill amount {Amount}", walletId, bill.Amount);
                throw new InvalidOperationException("Insufficient funds");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                wallet.Balance -= bill.Amount;
                wallet.LastUpdated = DateTime.UtcNow;
                bill.Status = BillStatus.Paid;
                bill.PaidAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Payment processed for bill {BillId}, wallet {WalletId}", bill.Id, wallet.Id);

                var provider = _providers.First(p => bill.MeterNumber.StartsWith(p.GetType().Name[8].ToString()));
                var token = await provider.GenerateTokenAsync(bill.MeterNumber, bill.Amount);

                await _eventPublisher.PublishAsync("payment_completed", new PaymentCompletedEvent
                {
                    BillId = bill.Id,
                    WalletId = wallet.Id,
                    Amount = bill.Amount,
                    Token = token,
                    Timestamp = DateTime.UtcNow
                });

                await _smsService.SendSmsAsync(
                    "dummy-phone",
                    $"Payment successful! Your token is: {token}"
                );
                bill.Token = token;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully for bill {BillId}", bill.Id);

                return token;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction failed for bill {BillId} with validation reference {ValidationRef}", bill.Id, validationRef);
                throw;
            }
        }
    }


}
