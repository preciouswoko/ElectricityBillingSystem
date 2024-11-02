using ElectricityBillingSystem.Domain.Events;
using ElectricityBillingSystem.Infrastructure.IServices;
using ElectricityBillingSystem.Infrastructure.Messaging.MockSqs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Infrastructure.Messaging.EventHandlers
{
    public class PaymentEventHandler : BackgroundService
    {
        private readonly IMockSqsQueue _queue;
        private readonly ISmsService _smsService;
        private readonly ILogger<PaymentEventHandler> _logger;

        public PaymentEventHandler(
            IMockSqsQueue queue,
            ISmsService smsService,
            ILogger<PaymentEventHandler> logger)
        {
            _queue = queue;
            _smsService = smsService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _queue.ReceiveMessageAsync<PaymentCompletedEvent>();
                    if (message != null)
                    {
                        await ProcessPaymentCompletedEvent(message.Payload);
                        await _queue.DeleteMessageAsync(message);
                    }

                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing payment events");
                }
            }
        }

        private async Task ProcessPaymentCompletedEvent(PaymentCompletedEvent evt)
        {
            _logger.LogInformation("Processing payment completed event: {@Event}", evt);
            await _smsService.SendSmsAsync(
                evt.PhoneNumber,
                $"Payment successful! Token: {evt.Token}"
            );
        }
    }
}
