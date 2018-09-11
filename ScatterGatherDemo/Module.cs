using Autofac;
using GreenPipes;
using MassTransit;
using ScatterGatherDemo.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScatterGatherDemo
{
    public class Module : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(ctx =>
            {
                return Bus.Factory.CreateUsingInMemory(cfg =>
                {
                    cfg.UseInMemoryScheduler();

                    cfg.ReceiveEndpoint("create_jobs", ec =>
                    {
                        ec.Consumer<IConsumer<CreateJobsCommand>>(ctx);
                        ec.UseScheduledRedelivery(c => c.Interval(100, TimeSpan.FromMilliseconds(200)));

                        /// we can use the outbox to ensure no messages sent from within the consumer,
                        /// are queued before the consumer completes successfully
                        /// we have other ideas in mind. see the consumer for more.
                        // ec.UseInMemoryOutbox();
                    });

                    cfg.ReceiveEndpoint("do_job", ec =>
                    {
                        ec.Consumer<IConsumer<DoJobCommand>>(ctx);
                        ec.UseScheduledRedelivery(c => c.Interval(100, TimeSpan.FromMilliseconds(200)));
                    });

                    cfg.ReceiveEndpoint("track_jobs", ec =>
                    {
                        ec.Consumer<IConsumer<TrackJobsCommand>>(ctx);
                        ec.UseScheduledRedelivery(c => c.Interval(100, TimeSpan.FromMilliseconds(200)));
                    });
                });
            })
            .SingleInstance()
            .As<IBusControl>()
            .As<IBus>();
        }
    }
}