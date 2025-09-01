using Microsoft.AspNetCore.Components;
using NycJobFilings.Data.Models;
using NycJobFilings.Data.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NycJobFilings.Web.Pages
{
    public partial class SavedFilters : ComponentBase
    {
        [Inject]
        protected FilterService FilterService { get; set; } = default!;
        
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        private List<FilterSet>? FilterSets { get; set; }
        private const string DemoUserId = "demo-user";

        protected override async Task OnInitializedAsync()
        {
            await LoadSavedFilters();
        }

        private async Task LoadSavedFilters()
        {
            // Load saved filters for demo user
            FilterSets = await FilterService.GetSavedFilterSetsAsync(DemoUserId);
        }

        private void ApplyFilter(FilterSet filterSet)
        {
            // In a real application, this would navigate to the job filings page with the filter applied
            // This could be done via query parameters or state management
            NavigationManager.NavigateTo("job-filings");
        }

        private async Task DeleteFilter(FilterSet filterSet)
        {
            if (filterSet.Id == null) return;

            // Delete the filter set
            await FilterService.DeleteFilterSetAsync(DemoUserId, filterSet.Id);
            
            // Reload the list
            await LoadSavedFilters();
        }
    }
}
