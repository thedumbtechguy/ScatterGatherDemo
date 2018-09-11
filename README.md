# ScatterGatherDemo
A simple implementation of the scatter gather pattern in dot net using MassTransit &amp; Redis

Sometimes, we need to process a bunch of tasks concurrently and do something right at the end.
Distributing this work over multiple processes helps speed up the process.

The problem comes when attempting to track the progress of the jobs. Massive concurrency can lead to serialization issues and excessive load on the data store.

In this implementation, we use Redis which handles load amazingly well and is serialized by default.
MassTransit allows us to abstract away our queuing infrastructure and gives us handy tools like retries and scheduling making things much easier.

After each job is en-queued, we add a unique identifier to a redis set.
After each job completes, we remove that item from the Redis set.
After all jobs are en-queued for processing, we start a tracking consumer to track the progress of the workers (inspired by https://lostechies.com/jimmybogard/2014/02/27/reducing-nservicebus-saga-load/).

> This design suits our needs. For low load tasks, we can add all job identifiers and then proceed to en-queue the tasks.
> There is no need to check for duplicates now, but to make your application tolerant to this, each task should be idempotent.

### Tips

- You can also store data in Redis to be retrieved by the gathering service and used subsequently.
- Your initial scheduled time should consider how long you expect jobs to run for. This is just an optimization measure.
- Your subsequent polling (sleep) time should be how long you can afford between tasks completing and the final task running.

## Requirements

- Dot Net Core 2.1
- C# 7.3
- Redis

## Running

You need to configure `Redis` in the `appsettings.Development.json` to point to your instance.
The demo uses an in memory scheduler and queue so no need for an external queue service. You can easily adapt this to suit your needs.

Just easier to run in the latest visual studio.
