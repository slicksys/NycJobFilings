using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using NycJobFilings.Data;
using NycJobFilings.Data.Services;
using NycJobFilings.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Configure SQL Server
builder.Services.AddDbContext<JobFilingsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=(localdb)\\mssqllocaldb;Database=NycJobFilings;Trusted_Connection=True;MultipleActiveResultSets=true"));

// Add application services
builder.Services.AddScoped<JobFilingService>();
builder.Services.AddColumnMetadataService(builder.Configuration);
builder.Services.AddFilterService(builder.Configuration);
builder.Services.AddProgressiveLoadingService();

// Add DevExpress Blazor services
builder.Services.AddDevExpressBlazor(configure => configure.SizeMode = DevExpress.Blazor.SizeMode.Medium);

// For development/demo, keep the sample weather service
builder.Services.AddSingleton<WeatherForecastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
