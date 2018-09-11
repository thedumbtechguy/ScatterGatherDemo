using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScatterGatherDemo.Contracts;
using ScatterGatherDemo.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScatterGatherDemo.Consumers
{
    internal class DoJobCommandConsumer : IConsumer<DoJobCommand>
    {
        private readonly ILogger<DoJobCommandConsumer> _logger;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RedisOptions _redisOptions;

        public DoJobCommandConsumer(
            ILogger<DoJobCommandConsumer> logger,
            IConnectionMultiplexer connectionMultiplexer,
            IOptions<RedisOptions> redisOptions)
        {
            _logger = logger;
            _connectionMultiplexer = connectionMultiplexer;
            _redisOptions = redisOptions.Value;
        }

        public async Task Consume(ConsumeContext<DoJobCommand> context)
        {
            var redisDB = _connectionMultiplexer.GetDatabase(_redisOptions.Database);

            /// let's fake some work
            await Task.Delay(new Random().Next(50, 1000)).ConfigureAwait(false);
            await redisDB.SetRemoveAsync($"{context.Message.CorrelationId}:JobList", context.Message.Job).ConfigureAwait(false);

            //_logger.LogInformation("Processed {Job}", context.Message.Job);
        }
    }
}