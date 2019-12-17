using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WB.Services.Scheduler.Model;
using WB.Services.Scheduler.Model.Events;

namespace WB.Services.Scheduler.Services.Implementation
{
    internal class JobProgressReporter : BackgroundService, IJobProgressReporter
    {
        private readonly IJobCancellationNotifier jobCancellationNotifier;
        private readonly ILogger<JobProgressReporter> logger;
        private readonly TaskCompletionSource<bool> queueCompletion = new TaskCompletionSource<bool>();

        public JobProgressReporter(IServiceProvider serviceProvider, IJobCancellationNotifier jobCancellationNotifier, ILogger<JobProgressReporter> logger)
        {
            this.jobCancellationNotifier = jobCancellationNotifier;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                foreach (var task in queue.GetConsumingEnumerable())
                {
                    try
                    {
                        using var scope = serviceProvider.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<JobContext>();

                        await using var tr = await db.Database.BeginTransactionAsync(stoppingToken);
                        var job = await db.Jobs.FindAsync(task.Id);

                        job.Handle(task);
                        db.Jobs.Update(job);

                        if (task is CancelJobEvent)
                        {
                            await jobCancellationNotifier.NotifyOnJobCancellationAsync(job.Id);
                        }

                        await db.SaveChangesAsync(stoppingToken);
                        logger.LogTrace(task.ToString());
                        await tr.CommitAsync(stoppingToken);
                    }
                    catch (Exception e)
                    {
                        logger.LogError("Progress reporting queue got an error", e);
                    }
                }

                queueCompletion.SetResult(true);
            }, stoppingToken);
        }

        public void CompleteJob(long jobId)
        {
            if (!queue.IsAddingCompleted)
                queue.Add(new CompleteJobEvent(jobId));
        }

        public void FailJob(long jobId, Exception exception)
        {
            if (!queue.IsAddingCompleted)
                queue.Add(new FailJobEvent(jobId, exception));
        }

        public void UpdateJobData(long jobId, string key, object value)
        {
            if (!queue.IsAddingCompleted)
                queue.Add(new UpdateDataEvent(jobId, key, value));
        }

        public void CancelJob(long jobId, string reason)
        {
            if (!queue.IsAddingCompleted)
                queue.Add(new CancelJobEvent(jobId, reason));
        }

        public Task AbortAsync(CancellationToken cancellationToken)
        {
            queue.CompleteAdding();
            queueCompletion.Task.Wait(TimeSpan.FromSeconds(5)); // waiting at least 5 seconds to complete queue
            return Task.CompletedTask;
        }

        readonly BlockingCollection<IJobEvent> queue = new BlockingCollection<IJobEvent>();
        private readonly IServiceProvider serviceProvider;

        public void Dispose()
        {
            if (!queue.IsCompleted)
                queue.CompleteAdding();
        }
    }
}
