using ElectricityBillingSystem.Domain.Enums;
using ElectricityBillingSystem.Domain.Models;
using ElectricityBillingSystem.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Infrastructure.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Check if context is null
            if (context == null)
            {
                Console.WriteLine("AppDbContext could not be retrieved.");
                return;
            }

            // Ensure the database is created and migrations are applied
            await context.Database.MigrateAsync();

            // Clear existing data only if data exists in each table
            try
            {
               // bool hasBills = context.Bills.Any();
                bool hasWallets = context.Wallets.Any();
                bool hasUsers = context.Users.Any();

                if ( /*hasBills ||*/ hasWallets || hasUsers)
                {
                    Console.WriteLine("Clearing existing data...");
                   // if (hasBills) context.Bills.RemoveRange(context.Bills);
                    if (hasWallets) context.Wallets.RemoveRange(context.Wallets);
                    if (hasUsers) context.Users.RemoveRange(context.Users);
                    await context.SaveChangesAsync();
                }
                else
                {
                    Console.WriteLine("No existing data to clear.");
                }

                // Seed new data
                await SeedUsersWithWalletsAsync(context);
               // await SeedBillsAsync(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during database seeding: " + ex.Message);
            }
        }

        private static async Task SeedUsersWithWalletsAsync(AppDbContext context)
        {
            if (context.Users.Any())
            {
                Console.WriteLine("Users already exist in the database.");
                return;
            }

            var users = new[]
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Name = "John Doe",
                    Email = "john.doe@example.com",
                    PhoneNumber = "+2348054980065",
                    Wallet = new Wallet
                    {
                        Balance = 500.00m
                    },
                    PasswordHash = Helper.ComputeSHA256Hash("Password123!")
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Name = "Jane Smith",
                    Email = "jane.smith@example.com",
                    PhoneNumber = "+2349029849684",
                    Wallet = new Wallet
                    {
                        Balance = 1000.00m
                    },
                    PasswordHash = Helper.ComputeSHA256Hash("Password1234!")
                }
            };

            users[0].Wallet.UserId = users[0].Id;
            users[1].Wallet.UserId = users[1].Id;

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        private static async Task SeedBillsAsync(AppDbContext context)
        {
            if (context.Bills.Any())
            {
                Console.WriteLine("Bills already exist in the database.");
                return;
            }

            var users = await context.Users.ToListAsync();
            if (!users.Any())
            {
                Console.WriteLine("No users found to associate with bills.");
                return;
            }

            var bills = new[]
            {
                new Bill
                {
                    Amount = 150.00m,
                    Status = BillStatus.Pending,
                    CustomerName = users.First().Name
                },
                new Bill
                {
                    Amount = 200.00m,
                    Status = BillStatus.Pending,
                    CustomerName = users.Last().Name
                }
            };

            await context.Bills.AddRangeAsync(bills);
            await context.SaveChangesAsync();
        }
    }
}
