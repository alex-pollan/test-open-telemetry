using System.Collections.Generic;

namespace Core
{
    public abstract class RabbitMqOperation
    {
    }

    public class RabbitMqPublishOperation : RabbitMqOperation
    {
        public string ExchangeName { get; set; }
        public string RoutingKey { get; set; }
        public bool IsConfirmed { get; set; }
        public IDictionary<string, object> Headers { get; set; }
    }
}