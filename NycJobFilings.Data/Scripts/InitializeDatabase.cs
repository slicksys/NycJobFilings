using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NycJobFilings.Data;
using NycJobFilings.Data.Scripts;

namespace NycJobFilings.Data.Scripts
{
    public class InitializeDatabaseScript
    {
        public static async Task RunAsync(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Add DbContext
                    services.AddDbContext<JobFilingsDbContext>(options =>
                        options.UseSqlServer(
                            context.Configuration.GetConnectionString("DefaultConnection") 
                            ?? "Server=(localdb)\\mssqllocaldb;Database=NycJobFilings;Trusted_Connection=True;MultipleActiveResultSets=true"));
                    
                    services.AddHttpClient();
                    services.AddTransient<DataImporter>();
                })
                .Build();

            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<InitializeDatabaseScript>>();

            try
            {
                logger.LogInformation("Initializing database");
                var dbContext = services.GetRequiredService<JobFilingsDbContext>();
                dbContext.Database.EnsureCreated();
                var importer = services.GetRequiredService<DataImporter>();
                if (args.Length > 0)
                {
                    switch (args[0].ToLower())
                    {
                        case "api":
                            // Import from API
                            var limit = args.Length > 1 && int.TryParse(args[1], out var l) ? l : 10000;
                            var monthsBack = args.Length > 2 && int.TryParse(args[2], out var m) ? m : 12;
                            
                            logger.LogInformation("Importing {Limit} records from the last {MonthsBack} months", limit, monthsBack);
                            await importer.ImportFromApiAsync(limit, monthsBack);
                            break;
                            
                        case "csv":
                            // Import from CSV
                            var filePath = args.Length > 1 ? args[1] : null;
                            
                            if (string.IsNullOrEmpty(filePath))
                            {
                                logger.LogError("No CSV file path provided");
                                return;
                            }
                            
                            logger.LogInformation("Importing data from CSV file: {FilePath}", filePath);
                            await importer.ImportFromCsvAsync(filePath);
                            break;
                            
                        case "test":
                            var count = args.Length > 1 && int.TryParse(args[1], out var c) ? c : 10000;
                            logger.LogInformation("Generating {Count} test records", count);
                            await importer.GenerateTestDataAsync(count);
                            break;
                            
                        default:
                            logger.LogError("Unknown command: {Command}", args[0]);
                            break;
                    }
                }
                else
                {
                    logger.LogInformation("No command specified, generating 10,000 test records");
                    await importer.GenerateTestDataAsync(10000);
                }
                
                logger.LogInformation("Database initialization completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database");
            }
        }
    }
}
