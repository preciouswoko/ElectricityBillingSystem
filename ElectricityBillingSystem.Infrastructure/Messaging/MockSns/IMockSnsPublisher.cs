using ElectricityBillingSystem.Infrastructure.Messaging.MockSqs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Infrastructure.Messaging.MockSns
{
    public interface IMockSnsPublisher
    {
        Task<string> PublishAsync<T>(string topicArn, T message);
        void Subscribe(string topicArn, IMockSqsQueue queue);
    }
}
