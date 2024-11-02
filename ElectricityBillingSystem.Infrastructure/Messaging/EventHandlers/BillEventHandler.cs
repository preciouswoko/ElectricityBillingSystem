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
    public class BillEventHandler : BackgroundService
    {
        private readonly IMockSqsQueue _queue;
        private readonly ISmsService _smsService;
        private readonly ILogger<BillEventHandler> _logger;

        public BillEventHandler(
            IMockSqsQueue queue,
            ISmsService smsService,
            ILogger<BillEventHandler> logger)
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
                    var message = await _queue.ReceiveMessageAsync<BillCreatedEvent>();
                    if (message != null)
                    {
                        await ProcessBillCreatedEvent(message.Payload);
                        await _queue.DeleteMessageAsync(message);
                    }

                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing bill events");
                }
            }
        }

        private async Task ProcessBillCreatedEvent(BillCreatedEvent evt)
        {
            _logger.LogInformation("Processing bill created event: {@Event}", evt);
            await _smsService.SendSmsAsync(
                "dummy-phone",
                $"New bill created: Amount {evt.Amount:C} for meter {evt.MeterNumber}"
            );
        }
    }
}
