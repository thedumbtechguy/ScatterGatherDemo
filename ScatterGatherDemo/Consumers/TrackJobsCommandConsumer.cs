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
    internal class TrackJobsCommandConsumer : IConsumer<TrackJobsCommand>
    {
        private readonly ILogger<TrackJobsCommandConsumer> _logger;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RedisOptions _redisOptions;

        public TrackJobsCommandConsumer(
            ILogger<TrackJobsCommandConsumer> logger,
            IConnectionMultiplexer connectionMultiplexer,
            IOptions<RedisOptions> redisOptions)
        {
            _logger = logger;
            _connectionMultiplexer = connectionMultiplexer;
            _redisOptions = redisOptions.Value;
        }

        public async Task Consume(ConsumeContext<TrackJobsCommand> context)
        {
            _logger.LogInformation("Checking for job progress");

            var redisDB = _connectionMultiplexer.GetDatabase(_redisOptions.Database);

            /// let's check if there are any jobs still left
            var remaining = await redisDB.SetLengthAsync($"{context.Message.CorrelationId}:JobList").ConfigureAwait(false);
            if(remaining == 0)
            {
                _logger.LogInformation("========Bingo! We are done here.");
            }
            else
            {
                _logger.LogInformation("{Count} unprocessed jobs", remaining);

                /// let's try again later. how long depends on your needs
                await context.Redeliver(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }
    }
}