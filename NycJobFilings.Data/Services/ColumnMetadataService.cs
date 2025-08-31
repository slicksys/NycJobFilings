using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace NycJobFilings.Data.Services
{
    public class ColumnMetadata
    {
        public string FieldName { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string? Description { get; set; }
        public bool Visible { get; set; } = true;
        public int DisplayOrder { get; set; }
        public bool Pinned { get; set; }
        public string DataType { get; set; } = "string";
        public bool IsFilterable { get; set; }
        public string? Format { get; set; }
        public int Width { get; set; } = 150;
    }

    public class UserColumnPreference
    {
        public string UserId { get; set; } = default!;
        public string FieldName { get; set; } = default!;
        public bool Visible { get; set; } = true;
        public int DisplayOrder { get; set; }
        public bool Pinned { get; set; }
        public int Width { get; set; } = 150;
    }

    public class ColumnMetadataService
    {
        private readonly string _metadataFilePath;
        private readonly string _userPreferencesDirectory;
        private readonly ILogger<ColumnMetadataService> _logger;

        public ColumnMetadataService(string metadataFilePath, string userPreferencesDirectory, ILogger<ColumnMetadataService> logger)
        {
            _metadataFilePath = metadataFilePath;
            _userPreferencesDirectory = userPreferencesDirectory;
            _logger = logger;

            // Ensure directories exist
            if (!Directory.Exists(Path.GetDirectoryName(metadataFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(metadataFilePath)!);
            }
            
            if (!Directory.Exists(userPreferencesDirectory))
            {
                Directory.CreateDirectory(userPreferencesDirectory);
            }
        }

        /// <summary>
        /// Get column metadata with user preferences applied if available
        /// </summary>
        public async Task<List<ColumnMetadata>> GetColumnMetadataAsync(string? userId = null)
        {
            try
            {
                // Load base metadata
                var metadata = await LoadMetadataAsync();

                // Apply user preferences if available
                if (!string.IsNullOrEmpty(userId))
                {
                    var preferences = await LoadUserPreferencesAsync(userId);
                    ApplyUserPreferences(metadata, preferences);
                }

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading column metadata");
                return GetDefaultMetadata();
            }
        }

        /// <summary>
        /// Save user preferences for columns
        /// </summary>
        public async Task SaveUserPreferencesAsync(string userId, List<UserColumnPreference> preferences)
        {
            var filePath = GetUserPreferencesPath(userId);
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(preferences));
        }

        private async Task<List<ColumnMetadata>> LoadMetadataAsync()
        {
            if (!File.Exists(_metadataFilePath))
            {
                var defaultMetadata = GetDefaultMetadata();
                await File.WriteAllTextAsync(_metadataFilePath, JsonSerializer.Serialize(defaultMetadata));
                return defaultMetadata;
            }

            var content = await File.ReadAllTextAsync(_metadataFilePath);
            return JsonSerializer.Deserialize<List<ColumnMetadata>>(content) ?? GetDefaultMetadata();
        }

        private async Task<List<UserColumnPreference>> LoadUserPreferencesAsync(string userId)
        {
            var filePath = GetUserPreferencesPath(userId);
            
            if (!File.Exists(filePath))
            {
                return new List<UserColumnPreference>();
            }

            var content = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<UserColumnPreference>>(content) ?? new List<UserColumnPreference>();
        }

        private void ApplyUserPreferences(List<ColumnMetadata> metadata, List<UserColumnPreference> preferences)
        {
            foreach (var preference in preferences)
            {
                var column = metadata.FirstOrDefault(c => c.FieldName == preference.FieldName);
                if (column != null)
                {
                    column.Visible = preference.Visible;
                    column.DisplayOrder = preference.DisplayOrder;
                    column.Pinned = preference.Pinned;
                    column.Width = preference.Width;
                }
            }

            // Sort by display order
            metadata.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
        }

        private string GetUserPreferencesPath(string userId)
        {
            return Path.Combine(_userPreferencesDirectory, $"{userId}.json");
        }

        /// <summary>
        /// Default metadata if no configuration is found
        /// </summary>
        private List<ColumnMetadata> GetDefaultMetadata()
        {
            return new List<ColumnMetadata>
            {
                new ColumnMetadata { FieldName = "JobS1No", DisplayName = "Job S1 No", Description = "Job S1 Number (Primary Key)", DisplayOrder = 0, Pinned = true, IsFilterable = true },
                new ColumnMetadata { FieldName = "Borough", DisplayName = "Borough", Description = "Borough where the job is located", DisplayOrder = 1, IsFilterable = true },
                new ColumnMetadata { FieldName = "HouseNo", DisplayName = "House No", Description = "House number", DisplayOrder = 2 },
                new ColumnMetadata { FieldName = "StreetName", DisplayName = "Street Name", Description = "Street name", DisplayOrder = 3 },
                new ColumnMetadata { FieldName = "Block", DisplayName = "Block", Description = "Block number", DisplayOrder = 4 },
                new ColumnMetadata { FieldName = "Lot", DisplayName = "Lot", Description = "Lot number", DisplayOrder = 5 },
                new ColumnMetadata { FieldName = "JobType", DisplayName = "Job Type", Description = "Type of job filing", DisplayOrder = 6, IsFilterable = true },
                new ColumnMetadata { FieldName = "JobStatus", DisplayName = "Job Status", Description = "Current status of the job", DisplayOrder = 7, IsFilterable = true },
                new ColumnMetadata { FieldName = "LatestActionDate", DisplayName = "Latest Action Date", Description = "Date of the latest action", DisplayOrder = 8, DataType = "datetime", Format = "MM/dd/yyyy", IsFilterable = true },
                new ColumnMetadata { FieldName = "FilingDate", DisplayName = "Filing Date", Description = "Original filing date", DisplayOrder = 9, DataType = "datetime", Format = "MM/dd/yyyy" },
                new ColumnMetadata { FieldName = "InitialCost", DisplayName = "Initial Cost", Description = "Initial cost of the project", DisplayOrder = 10, DataType = "decimal", Format = "C2", IsFilterable = true },
                new ColumnMetadata { FieldName = "ProposedDwellingUnits", DisplayName = "Proposed Dwelling Units", Description = "Number of proposed dwelling units", DisplayOrder = 11, DataType = "int", IsFilterable = true }
            };
        }
    }

    // Extension for DI setup
    public static class ColumnMetadataServiceExtensions
    {
        public static IServiceCollection AddColumnMetadataService(this IServiceCollection services, IConfiguration configuration)
        {
            var metadataPath = configuration["ColumnMetadata:FilePath"] ?? Path.Combine(AppContext.BaseDirectory, "Data", "column-metadata.json");
            var preferencesDir = configuration["ColumnMetadata:PreferencesDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "Data", "UserPreferences");

            services.AddSingleton<ColumnMetadataService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ColumnMetadataService>>();
                return new ColumnMetadataService(metadataPath, preferencesDir, logger);
            });

            return services;
        }
    }
}
