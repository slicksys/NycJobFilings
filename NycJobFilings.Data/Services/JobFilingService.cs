using Microsoft.EntityFrameworkCore;
using NycJobFilings.Data.Models;
using System.Linq.Expressions;

namespace NycJobFilings.Data.Services
{
    public class JobFilingService
    {
        private readonly JobFilingsDbContext _context;

        public JobFilingService(JobFilingsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets a paginated list of job filings with optional filtering
        /// </summary>
        /// <param name="page">Current page</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="filter">Optional filter expression</param>
        /// <param name="visibleColumns">Columns to select (optimization for Option C)</param>
        /// <param name="cancellationToken">Cancellation token for cancelling the operation</param>
        /// <returns>Paginated job filings</returns>
        public async Task<(IEnumerable<JobFiling> Items, int TotalCount)> GetJobFilingsAsync(
            int page = 1, 
            int pageSize = 1000,
            Expression<Func<JobFiling, bool>>? filter = null,
            IEnumerable<string>? visibleColumns = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<JobFiling> query = _context.JobFilings;

            // Apply filter if provided
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // Get total count for pagination
            int totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var pagedItems = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (pagedItems, totalCount);
        }

        /// <summary>
        /// Gets job filings in batches for progressive loading
        /// </summary>
        /// <param name="batchSize">Size of each batch</param>
        /// <param name="filter">Optional filter expression</param>
        /// <param name="visibleColumns">Columns to select (optimization)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Job filings in batches</returns>
        public async IAsyncEnumerable<IEnumerable<JobFiling>> GetJobFilingsBatchesAsync(
            int batchSize = 5000,
            Expression<Func<JobFiling, bool>>? filter = null,
            IEnumerable<string>? visibleColumns = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            IQueryable<JobFiling> query = _context.JobFilings;

            // Apply filter if provided
            if (filter != null)
            {
                query = query.Where(filter);
            }

            int totalCount = await query.CountAsync(cancellationToken);
            int totalBatches = (int)Math.Ceiling((double)totalCount / batchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                var batch = await query
                    .Skip(i * batchSize)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                yield return batch;
            }
        }

        /// <summary>
        /// Gets distinct values for a specific property, useful for filter chips
        /// </summary>
        /// <typeparam name="TProperty">Property type</typeparam>
        /// <param name="propertySelector">Property selector</param>
        /// <returns>Distinct values</returns>
        public async Task<IEnumerable<TProperty>> GetDistinctValuesAsync<TProperty>(
            Expression<Func<JobFiling, TProperty>> propertySelector)
        {
            return await _context.JobFilings
                .Select(propertySelector)
                .Distinct()
                .Where(x => x != null)
                .Take(1000) // Limit the number of distinct values
                .ToListAsync();
        }
    }
}
