namespace MemGPT
{
    using System;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class MemoryDeletionWorker(IServiceProvider serviceProvider, ILogger<MemoryDeletionWorker> logger) : BackgroundService, IMemoryDeletionWorker
    {
        private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false
        });

        public bool Enqueue(string userId) => !string.IsNullOrWhiteSpace(userId) && _channel.Writer.TryWrite(userId);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Starting Background MemoryDeletionWorker");
            var reader = _channel.Reader;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    while (await reader.WaitToReadAsync(stoppingToken) && reader.TryRead(out var userId))
                    {
                        logger.LogDebug($"Processing memory deletion request, UserId: {userId}");
                        try
                        {
                            var memoryManager = serviceProvider.GetService<IMemoryManager>();
                            await memoryManager.DeleteAsync(userId, stoppingToken);

                            logger.LogInformation($"Successfully deleted memory, UserId: {userId}");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Error deleting memory, UserId: {userId}");
                        }
                    }
                }
                catch (OperationCanceledException e) when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation(e, "Stopping Background MemoryDeletionWorker due to exception");
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in Background MemoryDeletionWorker");
                }
            }

            logger.LogInformation("Background MemoryDeletionWorker has been stopped");
        }
    }
}