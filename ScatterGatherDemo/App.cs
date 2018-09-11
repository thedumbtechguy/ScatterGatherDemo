using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScatterGatherDemo.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScatterGatherDemo
{
    public class App : IHostedService
    {
        private readonly IBusControl _busControl;
        private readonly ILogger<App> _logger;

        public App(
            ILogger<App> logger,
            IBusControl busControl)
        {
            _busControl = busControl;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("How many jobs:");
            var response = Console.ReadLine();
            int.TryParse(response, out var n);
            n = n > 0 ? n : new Random().Next(100, 5000);

            _logger.LogDebug("Starting bus");
            await _busControl.StartAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Bus started");

            _logger.LogInformation("Testing with {Count} jobs", n);
            var endpoint = await _busControl.GetSendEndpoint(new Uri("loopback://localhost/create_jobs")).ConfigureAwait(false);
            await endpoint.Send(new CreateJobsCommand
            {
                CorrelationId = NewId.Next().ToString(),
                // ideally we wouldn't do this
                // instead we would retrieve jobs from a store inside the consumer
                // aim to keep message sizes small
                Jobs = Enumerable.Range(0, n).Select(_ => NewId.NextGuid().ToString()).ToArray(),
            }).ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Stopping bus");
            await _busControl.StopAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Bus stopped");
        }
    }
}