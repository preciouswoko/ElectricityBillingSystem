using ElectricityBillingSystem.Infrastructure.Messaging.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Infrastructure.Messaging.MockSqs
{
    public class MockSqsQueue : IMockSqsQueue
    {
        private readonly ConcurrentQueue<object> _messages = new();
        private readonly ILogger<MockSqsQueue> _logger;
        public string QueueUrl { get; }

        public MockSqsQueue(string queueUrl, ILogger<MockSqsQueue> logger)
        {
            QueueUrl = queueUrl;
            _logger = logger;
        }

        public void EnqueueMessage<T>(Message<T> message)
        {
            message.QueueUrl = QueueUrl;
            _messages.Enqueue(message);
            _logger.LogInformation("Message enqueued to {QueueUrl}: {@Message}", QueueUrl, message);
        }

        public Task<Message<T>> ReceiveMessageAsync<T>()
        {
            if (_messages.TryDequeue(out var message))
            {
                _logger.LogInformation("Message dequeued from {QueueUrl}: {@Message}", QueueUrl, message);
                return Task.FromResult((Message<T>)message);
            }
            return Task.FromResult<Message<T>>(null);
        }

        public Task DeleteMessageAsync<T>(Message<T> message)
        {
            _logger.LogInformation("Message deleted from {QueueUrl}: {MessageId}", QueueUrl, message.MessageId);
            return Task.CompletedTask;
        }
    }
}
