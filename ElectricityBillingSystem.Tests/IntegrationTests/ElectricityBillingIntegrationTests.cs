using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using ElectricityBillingSystem.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using ElectricityBillingSystem.Domain.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace ElectricityBillingSystem.Tests.IntegrationTests
{
    public class ElectricityBillingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ElectricityBillingIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace real db context with in-memory database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });

                    // Seed test data
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    db.Wallets.Add(new Wallet
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Balance = 1000.00m
                    });

                    db.SaveChanges();
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CompletePaymentFlow_Success()
        {
            // Step 1: Verify Bill
            var verifyResponse = await _client.PostAsJsonAsync("/electricity/verify", new
            {
                meterNumber = "A123456",
                amount = 100.00
            });

            verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var verifyContent = await verifyResponse.Content.ReadFromJsonAsync<dynamic>();
            string validationRef = verifyContent.validationRef;

            // Step 2: Add funds to wallet
            var walletId = "11111111-1111-1111-1111-111111111111";
            var addFundsResponse = await _client.PostAsJsonAsync($"/wallets/{walletId}/add-funds", new
            {
                amount = 200.00
            });

            addFundsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 3: Process Payment
            _client.DefaultRequestHeaders.Add("X-Wallet-Id", walletId);
            var paymentResponse = await _client.PostAsync(
                $"/electricity/Vend/{validationRef}/pay",
                null
            );

            paymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var paymentContent = await paymentResponse.Content.ReadFromJsonAsync<dynamic>();
            paymentContent.token.Should().NotBeNull();
        }
    }
}
