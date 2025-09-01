using Microsoft.AspNetCore.Components;
using NycJobFilings.Data.Models;
using NycJobFilings.Data.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NycJobFilings.Web.Pages
{
    public partial class ColumnPreferences : ComponentBase
    {
        [Inject]
        protected ColumnMetadataService ColumnMetadataService { get; set; } = default!;

        private List<ColumnMetadata>? Columns { get; set; }
        private const string DemoUserId = "demo-user";

        protected override async Task OnInitializedAsync()
        {
            // Load column metadata
            Columns = await ColumnMetadataService.GetColumnMetadataAsync(DemoUserId);
        }

        private async Task SavePreferences()
        {
            if (Columns == null) return;

            // Convert to user preferences
            var preferences = Columns.Select(c => new UserColumnPreference
            {
                UserId = DemoUserId,
                FieldName = c.FieldName,
                Visible = c.Visible,
                DisplayOrder = c.DisplayOrder,
                Pinned = c.Pinned,
                Width = c.Width
            }).ToList();

            // Save preferences
            await ColumnMetadataService.SaveUserPreferencesAsync(DemoUserId, preferences);

            // Show success message (in a real app, this would use a toast notification)
            await Task.Delay(100); // Simulate a short delay for saving
        }
    }
}
