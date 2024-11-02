using ElectricityBillingSystem.Domain.Events;
using ElectricityBillingSystem.Infrastructure.IServices;
using ElectricityBillingSystem.Infrastructure.Messaging.EventHandlers;
using ElectricityBillingSystem.Infrastructure.Messaging.MockSns;
using ElectricityBillingSystem.Infrastructure.Messaging.MockSqs;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ElectricityBillingSystem.Tests.IntegrationTests
{
    public class EventHandlingTests
    {
        private readonly Mock<ISmsService> _mockSmsService;
        private readonly MockSnsPublisher _publisher;
        private readonly MockSqsQueue _billsQueue;
        private readonly MockSqsQueue _paymentsQueue;
        private readonly BillEventHandler _billHandler;
        private readonly PaymentEventHandler _paymentHandler;

        public EventHandlingTests()
        {
            _mockSmsService = new Mock<ISmsService>();
            var logger = Mock.Of<ILogger<MockSnsPublisher>>();
            var queueLogger = Mock.Of<ILogger<MockSqsQueue>>();
            var billHandlerLogger = Mock.Of<ILogger<BillEventHandler>>();
            var paymentHandlerLogger = Mock.Of<ILogger<PaymentEventHandler>>();

            _publisher = new MockSnsPublisher(logger);
            _billsQueue = new MockSqsQueue("bills-queue", queueLogger);
            _paymentsQueue = new MockSqsQueue("payments-queue", queueLogger);

            _publisher.Subscribe("bill_created", _billsQueue);
            _publisher.Subscribe("payment_completed", _paymentsQueue);

            _billHandler = new BillEventHandler(_billsQueue, _mockSmsService.Object, billHandlerLogger);
            _paymentHandler = new PaymentEventHandler(_paymentsQueue, _mockSmsService.Object, paymentHandlerLogger);
        }

        [Fact]
        public async Task EventFlow_ProcessesEventsAndSendsNotifications()
        {
            // Arrange
            var billEvent = new BillCreatedEvent
            {
                BillId = Guid.NewGuid(),
                Amount = 100.00m,
                MeterNumber = "A123456"
            };

            var paymentEvent = new PaymentCompletedEvent
            {
                BillId = Guid.NewGuid(),
                Amount = 100.00m,
                Token = "TEST-TOKEN"
            };

            // Act - Publish events
            await _publisher.PublishAsync("bill_created", billEvent);
            await _publisher.PublishAsync("payment_completed", paymentEvent);

            // Start handlers
            using var billCts = new CancellationTokenSource();
            using var paymentCts = new CancellationTokenSource();

            var billTask = _billHandler.StartAsync(billCts.Token);
            var paymentTask = _paymentHandler.StartAsync(paymentCts.Token);

            // Let handlers process messages
            await Task.Delay(2000);

            // Stop handlers
            //await billCts..CancelAsync();
            //await paymentCts.CancelAsync();
             billCts.Cancel();
             paymentCts.Cancel();

            // Assert
            _mockSmsService.Verify(
                s => s.SendSmsAsync(
                    It.IsAny<string>(),
                    It.Is<string>(msg => msg.Contains(billEvent.MeterNumber))
                ),
                Times.Once
            );

            _mockSmsService.Verify(
                s => s.SendSmsAsync(
                    It.IsAny<string>(),
                    It.Is<string>(msg => msg.Contains(paymentEvent.Token))
                ),
                Times.Once
            );
        }
    }
}
