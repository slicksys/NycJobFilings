using NycJobFilings.Data.Models;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace NycJobFilings.Data.Services
{
    public class ProgressiveLoadingState
    {
        public int TotalRecords { get; set; }
        public int LoadedRecords { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ProgressiveLoadingService
    {
        private readonly JobFilingService _jobFilingService;
        private readonly ILogger<ProgressiveLoadingService> _logger;
        private readonly Dictionary<string, CancellationTokenSource> _activeTasks = new();
        private readonly Dictionary<string, ProgressiveLoadingState> _loadingStates = new();

        public ProgressiveLoadingService(JobFilingService jobFilingService, ILogger<ProgressiveLoadingService> logger)
        {
            _jobFilingService = jobFilingService;
            _logger = logger;
        }

        /// <summary>
        /// Starts progressive loading of job filings
        /// </summary>
        /// <param name="loadingId">Unique ID for this loading operation</param>
        /// <param name="filter">Optional filter expression</param>
        /// <param name="visibleColumns">Optional visible columns to optimize the query</param>
        /// <param name="initialBatchSize">Size of the initial batch</param>
        /// <param name="subsequentBatchSize">Size of subsequent batches</param>
        /// <returns>Channel to read loaded data from</returns>
        public Task<(ChannelReader<IEnumerable<JobFiling>> DataChannel, string LoadingId)> StartProgressiveLoadingAsync(
            string? loadingId = null,
            System.Linq.Expressions.Expression<Func<JobFiling, bool>>? filter = null,
            IEnumerable<string>? visibleColumns = null,
            int initialBatchSize = 1000,
            int subsequentBatchSize = 5000)
        {
            loadingId ??= Guid.NewGuid().ToString();
            
            // Create a channel to communicate the data
            var channel = Channel.CreateBounded<IEnumerable<JobFiling>>(new BoundedChannelOptions(10)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });

            // Create cancellation token
            var cts = new CancellationTokenSource();
            _activeTasks[loadingId] = cts;

            // Initialize the loading state
            var loadingState = new ProgressiveLoadingState
            {
                StartTime = DateTime.UtcNow,
                LoadedRecords = 0,
                IsCompleted = false,
                IsCancelled = false
            };
            
            _loadingStates[loadingId] = loadingState;

            // Start the loading task
            Task.Run(async () =>
            {
                try
                {
                    // Load the initial batch (fast)
                    var initialBatch = await _jobFilingService.GetJobFilingsAsync(
                        page: 1,
                        pageSize: initialBatchSize,
                        filter: filter,
                        visibleColumns: visibleColumns,
                        cancellationToken: cts.Token);

                    // Update state with total records
                    loadingState.TotalRecords = initialBatch.TotalCount;
                    loadingState.LoadedRecords = initialBatch.Items.Count();

                    // Write the initial batch to the channel
                    await channel.Writer.WriteAsync(initialBatch.Items, cts.Token);

                    // If there's more data, load it progressively
                    if (loadingState.LoadedRecords < loadingState.TotalRecords)
                    {
                        await using var batches = _jobFilingService.GetJobFilingsBatchesAsync(
                            subsequentBatchSize,
                            filter,
                            visibleColumns, 
                            cts.Token)
                            .GetAsyncEnumerator(cts.Token);

                        // Skip the first batch as we already loaded it
                        int batchIndex = 1;
                        while (await batches.MoveNextAsync())
                        {
                            if (batchIndex > 0) // Skip the first batch
                            {
                                var batch = batches.Current;
                                loadingState.LoadedRecords += batch.Count();
                                await channel.Writer.WriteAsync(batch, cts.Token);
                            }
                            batchIndex++;
                        }
                    }

                    // Complete the operation successfully
                    loadingState.IsCompleted = true;
                    loadingState.EndTime = DateTime.UtcNow;
                    channel.Writer.Complete();
                }
                catch (OperationCanceledException)
                {
                    loadingState.IsCancelled = true;
                    loadingState.EndTime = DateTime.UtcNow;
                    channel.Writer.Complete();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during progressive loading for ID {LoadingId}", loadingId);
                    loadingState.ErrorMessage = ex.Message;
                    loadingState.EndTime = DateTime.UtcNow;
                    channel.Writer.Complete(ex);
                }
                finally
                {
                    // Clean up resources
                    _activeTasks.Remove(loadingId);
                }
            });

            return Task.FromResult((channel.Reader, loadingId));
        }

        /// <summary>
        /// Cancels an active loading operation
        /// </summary>
        public void CancelLoading(string loadingId)
        {
            if (_activeTasks.TryGetValue(loadingId, out var cts))
            {
                cts.Cancel();
            }
        }

        /// <summary>
        /// Gets the current state of a loading operation
        /// </summary>
        public ProgressiveLoadingState? GetLoadingState(string loadingId)
        {
            return _loadingStates.TryGetValue(loadingId, out var state) ? state : null;
        }

        /// <summary>
        /// Cleans up completed or stale loading operations
        /// </summary>
        public void CleanupOldLoadingStates(TimeSpan olderThan)
        {
            var cutoff = DateTime.UtcNow - olderThan;
            var toRemove = _loadingStates
                .Where(kvp => 
                    kvp.Value.IsCompleted || 
                    kvp.Value.IsCancelled || 
                    (kvp.Value.EndTime.HasValue && kvp.Value.EndTime < cutoff))
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in toRemove)
            {
                _loadingStates.Remove(key);
            }
        }
    }

    // Extension for DI setup
    public static class ProgressiveLoadingServiceExtensions
    {
        public static IServiceCollection AddProgressiveLoadingService(this IServiceCollection services)
        {
            services.AddSingleton<ProgressiveLoadingService>();
            return services;
        }
    }
}
