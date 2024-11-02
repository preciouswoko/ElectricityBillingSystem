using ElectricityBillingSystem.API.Controllers;
using ElectricityBillingSystem.Application.Abstraction;
using ElectricityBillingSystem.Application.DTOs;
using ElectricityBillingSystem.Domain.Enums;
using ElectricityBillingSystem.Domain.Events;
using ElectricityBillingSystem.Domain.Models;
using ElectricityBillingSystem.Infrastructure.Data;
using ElectricityBillingSystem.Infrastructure.IServices;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElectricityBillingSystem.Tests.UnitTests.Controllers
{
    public class ElectricityControllerTests
    {
        private readonly Mock<AppDbContext> _mockContext;
        private readonly Mock<IEventPublisher> _mockEventPublisher;
        private readonly Mock<ISmsService> _mockSmsService;
        private readonly Mock<IElectricityProvider> _mockProviderA;
        private readonly Mock<IElectricityProvider> _mockProviderB;
        private readonly ElectricityController _controller;

        public ElectricityControllerTests()
        {
            _mockContext = new Mock<AppDbContext>();
            _mockEventPublisher = new Mock<IEventPublisher>();
            _mockSmsService = new Mock<ISmsService>();
            _mockProviderA = new Mock<IElectricityProvider>();
            _mockProviderB = new Mock<IElectricityProvider>();

            var mockElectricityService = new Mock<IElectricityService>();
            var mockLogger = new Mock<ILogger<ElectricityController>>();

            _controller = new ElectricityController(
                _mockContext.Object,
                mockElectricityService.Object,
                mockLogger.Object
            );
        }

        private Mock<DbSet<T>> CreateMockDbSet<T>(IEnumerable<T> data) where T : class
        {
            var queryableData = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryableData.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryableData.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryableData.GetEnumerator());

            return mockSet;
        }

        [Fact]
        public async Task VerifyBill_WithValidMeter_ReturnsValidationReference()
        {
            var request = new BillVerificationRequest
            {
                MeterNumber = "A123456",
                Amount = 100.00m
            };

            _mockProviderA.Setup(p => p.ValidateMeterAsync(request.MeterNumber))
                .ReturnsAsync((true, "John Doe"));

            var bills = CreateMockDbSet(new List<Bill>());
            _mockContext.Setup(c => c.Bills).Returns(bills.Object);

            var result = await _controller.VerifyBill(request);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<dynamic>().Subject;
            response.validationRef.Should().NotBeNull();

            _mockEventPublisher.Verify(
                e => e.PublishAsync(
                    It.Is<string>(s => s == "bill_created"),
                    It.IsAny<BillCreatedEvent>()
                ),
                Times.Once
            );

            _mockSmsService.Verify(
                s => s.SendSmsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task VerifyBill_WithInvalidMeter_ReturnsBadRequest()
        {
            var request = new BillVerificationRequest
            {
                MeterNumber = "X123456",
                Amount = 100.00m
            };

            _mockProviderA.Setup(p => p.ValidateMeterAsync(request.MeterNumber))
                .ReturnsAsync((false, string.Empty));
            _mockProviderB.Setup(p => p.ValidateMeterAsync(request.MeterNumber))
                .ReturnsAsync((false, string.Empty));

            var result = await _controller.VerifyBill(request);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProcessPayment_WithSufficientFunds_ReturnsToken()
        {
            var validationRef = "test-ref";
            var bill = new Bill
            {
                Id = Guid.NewGuid(),
                ValidationReference = validationRef,
                Amount = 100.00m,
                Status = BillStatus.Pending,
                MeterNumber = "A123456"
            };

            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                Balance = 200.00m
            };

            var bills = CreateMockDbSet(new[] { bill });
            var wallets = CreateMockDbSet(new[] { wallet });

            _mockContext.Setup(c => c.Bills).Returns(bills.Object);
            _mockContext.Setup(c => c.Wallets).Returns(wallets.Object);

            _mockProviderA.Setup(p => p.GenerateTokenAsync(bill.MeterNumber, bill.Amount))
                .ReturnsAsync("TEST-TOKEN");

            var result = await _controller.ProcessPayment(validationRef);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<dynamic>().Subject;
            response.token.Should().Be("TEST-TOKEN");

            _mockEventPublisher.Verify(
                e => e.PublishAsync(
                    It.Is<string>(s => s == "payment_completed"),
                    It.IsAny<PaymentCompletedEvent>()
                ),
                Times.Once
            );
        }
    }
}
