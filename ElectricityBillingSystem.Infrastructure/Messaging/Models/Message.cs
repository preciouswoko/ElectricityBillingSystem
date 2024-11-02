using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Infrastructure.Messaging.Models
{
    public class Message<T>
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string TopicArn { get; set; }
        public string QueueUrl { get; set; }
        public T Payload { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public IDictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}
