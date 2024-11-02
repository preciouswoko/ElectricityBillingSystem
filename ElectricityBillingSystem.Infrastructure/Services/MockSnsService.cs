using ElectricityBillingSystem.Infrastructure.IServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Infrastructure.Services
{
   
    public class MockSnsService : IEventPublisher
    {
        private readonly ILogger<MockSnsService> _logger;

        public MockSnsService(ILogger<MockSnsService> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<T>(string topic, T message)
        {
            _logger.LogInformation("Publishing message to topic {Topic}: {@Message}", topic, message);
            return Task.CompletedTask;
        }
    }
}
