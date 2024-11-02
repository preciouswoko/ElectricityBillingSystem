using ElectricityBillingSystem.Domain.Enums;
using ElectricityBillingSystem.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Bill> Bills { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<User> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bill>()
                .Property(b => b.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasPrecision(18, 2);
        }
        //public static void SeedData(AppDbContext context)
        //{
        //    // Clear existing data
        //    context.Bills.RemoveRange(context.Bills);
        //    context.Wallets.RemoveRange(context.Wallets);
        //    context.Users.RemoveRange(context.Users);

        //    // Seed users with wallets
        //    var users = new[]
        //    {
        //        new User
        //        {
        //            Id = Guid.NewGuid(),
        //            Name = "John Doe",
        //            Email = "john.doe@example.com",
        //            PhoneNumber = "+1234567890",
        //            Wallet = new Wallet
        //            {
        //                Balance = 500.00m
        //            }
        //        },
        //        new User
        //        {
        //            Id = Guid.NewGuid(),
        //            Name = "Jane Smith",
        //            Email = "jane.smith@example.com",
        //            PhoneNumber = "+0987654321",
        //            Wallet = new Wallet
        //            {
        //                Balance = 1000.00m
        //            }
        //        }
        //    };

        //    // Set wallet user ids
        //    users[0].Wallet.UserId = users[0].Id;
        //    users[1].Wallet.UserId = users[1].Id;

        //    context.Users.AddRange(users);

        //    // Seed some initial bills
        //    var bills = new[]
        //    {
        //        new Bill
        //        {
        //            Amount = 150.00m,
        //            Status = BillStatus.Pending,
        //            CustomerName = users[0].Name
        //        },
        //        new Bill
        //        {
        //            Amount = 200.00m,
        //            Status = BillStatus.Pending,
        //            CustomerName = users[1].Name
        //        }
        //    };

        //    context.Bills.AddRange(bills);

        //    context.SaveChanges();
        //}
    
}
}
