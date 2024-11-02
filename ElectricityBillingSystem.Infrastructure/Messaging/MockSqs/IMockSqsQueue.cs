using ElectricityBillingSystem.Infrastructure.Messaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Infrastructure.Messaging.MockSqs
{
    public interface IMockSqsQueue
    {
        void EnqueueMessage<T>(Message<T> message);
        Task<Message<T>> ReceiveMessageAsync<T>();
        Task DeleteMessageAsync<T>(Message<T> message);
    }
}
