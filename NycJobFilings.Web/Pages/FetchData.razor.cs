using Microsoft.AspNetCore.Components;
using NycJobFilings.Web.Data;
using System;
using System.Threading.Tasks;

namespace NycJobFilings.Web.Pages
{
    public partial class FetchData : ComponentBase
    {
        [Inject]
        protected WeatherForecastService ForecastService { get; set; } = default!;

        private WeatherForecast[]? forecasts;

        protected override async Task OnInitializedAsync()
        {
            forecasts = await ForecastService.GetForecastAsync(DateOnly.FromDateTime(DateTime.Now));
        }
    }
}
