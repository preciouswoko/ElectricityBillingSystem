using ElectricityBillingSystem.Infrastructure.Messaging.MockSqs;
using ElectricityBillingSystem.Infrastructure.Messaging.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Infrastructure.Messaging.MockSns
{
    public class MockSnsPublisher : IMockSnsPublisher
    {
        private readonly ILogger<MockSnsPublisher> _logger;
        private readonly Dictionary<string, List<IMockSqsQueue>> _subscriptions = new();
        private readonly object _lock = new();

        public MockSnsPublisher(ILogger<MockSnsPublisher> logger)
        {
            _logger = logger;
        }

        public Task<string> PublishAsync<T>(string topicArn, T message)
        {
            _logger.LogInformation("Publishing message to SNS topic {TopicArn}: {@Message}", topicArn, message);

            var msg = new Message<T>
            {
                TopicArn = topicArn,
                Payload = message
            };

            lock (_lock)
            {
                if (_subscriptions.TryGetValue(topicArn, out var queues))
                {
                    foreach (var queue in queues)
                    {
                        queue.EnqueueMessage(msg);
                    }
                }
            }

            return Task.FromResult(msg.MessageId);
        }

        public void Subscribe(string topicArn, IMockSqsQueue queue)
        {
            lock (_lock)
            {
                if (!_subscriptions.ContainsKey(topicArn))
                {
                    _subscriptions[topicArn] = new List<IMockSqsQueue>();
                }
                _subscriptions[topicArn].Add(queue);
            }
        }
    }
}
