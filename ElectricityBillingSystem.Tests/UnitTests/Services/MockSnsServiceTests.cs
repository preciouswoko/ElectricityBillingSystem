using ElectricityBillingSystem.Domain.Events;
using ElectricityBillingSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ElectricityBillingSystem.Tests.UnitTests.Services
{
    public class MockSnsServiceTests
    {
        private readonly Mock<ILogger<MockSnsService>> _mockLogger;
        private readonly MockSnsService _service;

        public MockSnsServiceTests()
        {
            _mockLogger = new Mock<ILogger<MockSnsService>>();
            _service = new MockSnsService(_mockLogger.Object);
        }

        [Fact]
        public async Task PublishAsync_PublishesMessageToSubscribers()
        {
            // Arrange
            var message = new BillCreatedEvent
            {
                BillId = Guid.NewGuid(),
                Amount = 100.00m
            };

            // Act
            await _service.PublishAsync("test-topic", message);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
                ),
                Times.Once
            );
        }
    }
}
