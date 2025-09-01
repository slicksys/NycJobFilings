using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using NycJobFilings.Data.Models;
using NycJobFilings.Data.Services;

namespace NycJobFilings.Web.Pages
{
    public partial class JobFilingsExplorer  : ComponentBase
    {
        [Inject] protected JobFilingService JobFilingService { get; set; } = default!;
        [Inject] protected ColumnMetadataService ColumnMetadataService { get; set; } = default!;
        [Inject] protected FilterService FilterService { get; set; } = default!;
        [Inject] protected ProgressiveLoadingService ProgressiveLoadingService { get; set; } = default!;

        protected List<JobFiling> JobFilings { get; set; } = new();
        protected List<ColumnMetadata> AllColumns { get; set; } = new();
        protected List<ColumnMetadata> VisibleColumns => AllColumns.Where(c => c.Visible).OrderBy(c => c.DisplayOrder).ToList();
        protected List<FilterCondition> ActiveFilters { get; set; } = new();
        protected string? LoadingId { get; set; }
        protected ProgressiveLoadingState? LoadingState { get; set; }
        protected bool IsLoading => LoadingState != null && !LoadingState.IsCompleted && !LoadingState.IsCancelled;
        protected int ProgressPercentage => LoadingState?.TotalRecords > 0
            ? (int)Math.Min(100, Math.Round((double)LoadingState.LoadedRecords / LoadingState.TotalRecords * 100))
            : 0;

        private Timer? _progressTimer;

        protected override async Task OnInitializedAsync()
        {
            AllColumns = await ColumnMetadataService.GetColumnMetadataAsync("demo-user");

            ActiveFilters.Add(new FilterCondition
            {
                FieldName = "LatestActionDate",
                Operator = ">=",
                Value = DateTime.Now.AddMonths(-12),
                DisplayText = "Latest Action Date ≥ 1 year ago"
            });

            await LoadDataAsync();
        }

        protected async Task LoadDataAsync()
        {
            try
            {
                JobFilings.Clear();

                var filterExpression = FilterService.BuildFilterExpression(ActiveFilters);
                var visibleColumnNames = VisibleColumns.Select(c => c.FieldName).ToList();

                var (channel, loadingId) = await ProgressiveLoadingService.StartProgressiveLoadingAsync(
                    filter: filterExpression,
                    visibleColumns: visibleColumnNames);

                LoadingId = loadingId;

                _progressTimer = new Timer(_ =>
                {
                    LoadingState = ProgressiveLoadingService.GetLoadingState(loadingId);
                    InvokeAsync(StateHasChanged);
                }, null, 0, 500);

                await foreach (var batch in channel.ReadAllAsync())
                {
                    JobFilings.AddRange(batch);
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading data: {ex.Message}");
            }
            finally
            {
                _progressTimer?.Dispose();
                _progressTimer = null;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void CancelLoading()
        {
            if (LoadingId != null)
            {
                ProgressiveLoadingService.CancelLoading(LoadingId);
            }
        }

        protected void AddNewFilter()
        {
            ActiveFilters.Add(new FilterCondition
            {
                FieldName = "Borough",
                Operator = "=",
                Value = "MANHATTAN",
                DisplayText = "Borough = MANHATTAN"
            });

            _ = LoadDataAsync();
        }

        protected void RemoveFilter(FilterCondition filter)
        {
            ActiveFilters.Remove(filter);
            _ = LoadDataAsync();
        }

        protected void ShowColumnChooser()
        {
            // Placeholder for column chooser dialog
        }

        protected void SaveCurrentFilterSet()
        {
            // Placeholder for save filter set dialog
        }

        protected object? GetPropertyValue(JobFiling filing, string propertyName)
        {
            var property = typeof(JobFiling).GetProperty(propertyName);
            if (property == null) return null;

            var value = property.GetValue(filing);

            if (value is DateTime dateValue)
            {
                return dateValue.ToString("MM/dd/yyyy");
            }
            else if (value is decimal decimalValue)
            {
                return decimalValue.ToString("C2");
            }

            return value;
        }
    }
}