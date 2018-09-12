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
    internal class CreateJobsCommandConsumer : IConsumer<CreateJobsCommand>
    {
        private readonly ILogger<CreateJobsCommandConsumer> _logger;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RedisOptions _redisOptions;

        public CreateJobsCommandConsumer(
            ILogger<CreateJobsCommandConsumer> logger,
            IConnectionMultiplexer connectionMultiplexer,
            IOptions<RedisOptions> redisOptions)
        {
            _logger = logger;
            _connectionMultiplexer = connectionMultiplexer;
            _redisOptions = redisOptions.Value;
        }

        public async Task Consume(ConsumeContext<CreateJobsCommand> context)
        {
            var redisDB = _connectionMultiplexer.GetDatabase(_redisOptions.Database);

            /// the reason we don't use the in-memory inbox is because for use we process thousands of jobs
            /// if the process fails, then we start all over again and lose any processing
            /// instead, we keep track of jobs that we've enqueued in a redis set
            /// if something goes wrong, we can skip what we've already processed
            /// choice is yours though how to go. your mileage may vary
            /// also, I have fud over what guarantees the inbox offers

            var alreadyEnqueued = (await redisDB.SetMembersAsync($"{context.Message.CorrelationId}:JobList").ConfigureAwait(false))
                .Select(m => (string) m)
                .ToHashSet();
            var jobs = alreadyEnqueued.Any()
                ? context.Message.Jobs.Where(j => !alreadyEnqueued.Contains(j)).ToArray()
                : context.Message.Jobs;

            _logger.LogInformation("Enqueuing jobs");
            var doJobEndpoint = await context.GetSendEndpoint(new Uri("loopback://localhost/do_job")).ConfigureAwait(false);
            foreach(var job in jobs)
            {
                /// if we want, we can add all items before we start
                /// we add the job id before enqueuing the job to prevent cases where the job finishes be we add it to the queue
                /// in this case, the id will never get removed and we would be stuck in limbo
                await redisDB.SetAddAsync($"{context.Message.CorrelationId}:JobList", job).ConfigureAwait(false);

                //_logger.LogInformation("Enqueued: {Job}", job);
                await doJobEndpoint.Send(new DoJobCommand
                {
                    CorrelationId = context.Message.CorrelationId,
                    Job = job,
                }).ConfigureAwait(false);
            }
            _logger.LogInformation("All jobs enqueued");

            _logger.LogInformation("Starting job tracker");
            /// no need to check immediately. let's give it some time
            var trackJobsEndpoint = await context.GetSendEndpoint(new Uri("loopback://localhost/quartz")).ConfigureAwait(false);
            await trackJobsEndpoint.ScheduleSend(new Uri("loopback://localhost/track_jobs"), TimeSpan.FromSeconds(2), new TrackJobsCommand
            {
                CorrelationId = context.Message.CorrelationId,
            }).ConfigureAwait(false);
            _logger.LogInformation("tracker started");
        }
    }
}