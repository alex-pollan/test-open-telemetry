using OpenTelemetry.Trace;
using System;

namespace Core
{
    public class RabbitMqCollectorOptions
    {
        private static readonly Func<RabbitMqOperation, ISampler> DefaultSampler = (req) => { return null; };

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestsCollectorOptions"/> class.
        /// </summary>
        /// <param name="sampler">Custom sampling function, if any.</param>
        public RabbitMqCollectorOptions(Func<RabbitMqOperation, ISampler> sampler = null)
        {
            this.CustomSampler = sampler ?? DefaultSampler;
        }

        /// <summary>
        /// Gets a hook to exclude calls based on domain
        /// or other per-request criterion.
        /// </summary>
        public Func<RabbitMqOperation, ISampler> CustomSampler { get; private set; }
    }
}