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
                new ColumnMetadata { FieldName = "JobDescription", DisplayName = "Job Description", Description = "Description of the job filing", DisplayOrder = 1, IsFilterable = true },
                new ColumnMetadata { FieldName = "Job", DisplayName = "Job #", Description = "Job number", DisplayOrder = 2, DataType = "number", IsFilterable = true },
                new ColumnMetadata { FieldName = "Doc", DisplayName = "Doc #", Description = "Document number", DisplayOrder = 3, DataType = "number", IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "Borough", DisplayName = "Borough", Description = "Borough where the job is located", DisplayOrder = 4, IsFilterable = true },
                new ColumnMetadata { FieldName = "HouseNo", DisplayName = "House #", Description = "House number", DisplayOrder = 5, Width = 100 },
                new ColumnMetadata { FieldName = "StreetName", DisplayName = "Street Name", Description = "Street name", DisplayOrder = 6, Width = 180 },
                new ColumnMetadata { FieldName = "Block", DisplayName = "Block", Description = "Block number", DisplayOrder = 7, DataType = "number", IsFilterable = true },
                new ColumnMetadata { FieldName = "Lot", DisplayName = "Lot", Description = "Lot number", DisplayOrder = 8, DataType = "number", IsFilterable = true },
                new ColumnMetadata { FieldName = "City", DisplayName = "City", Description = "City name", DisplayOrder = 9, IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "State", DisplayName = "State", Description = "State name", DisplayOrder = 10, IsFilterable = true, Visible = false, Width = 100 },
                new ColumnMetadata { FieldName = "Zip", DisplayName = "Zip", Description = "Zip code", DisplayOrder = 11, IsFilterable = true, Visible = false, Width = 100 },
                new ColumnMetadata { FieldName = "BuildingType", DisplayName = "Building Type", Description = "Type of building", DisplayOrder = 12, IsFilterable = true },
                new ColumnMetadata { FieldName = "JobType", DisplayName = "Job Type", Description = "Type of job filing", DisplayOrder = 13, IsFilterable = true },
                new ColumnMetadata { FieldName = "JobStatus", DisplayName = "Job Status", Description = "Current status of the job", DisplayOrder = 14, IsFilterable = true },
                new ColumnMetadata { FieldName = "JobStatusDescrp", DisplayName = "Job Status Description", Description = "Detailed description of job status", DisplayOrder = 15, Visible = false, Width = 200 },
                new ColumnMetadata { FieldName = "LatestActionDate", DisplayName = "Latest Action Date", Description = "Date of the latest action", DisplayOrder = 16, DataType = "datetime", Format = "MM/dd/yyyy", IsFilterable = true },
                new ColumnMetadata { FieldName = "PreFilingDate", DisplayName = "Pre-Filing Date", Description = "Date of pre-filing", DisplayOrder = 17, DataType = "datetime", Format = "MM/dd/yyyy", IsFilterable = true, Visible = false },
              //  new ColumnMetadata { FieldName = "FilingDate", DisplayName = "Filing Date", Description = "Date of filing", DisplayOrder = 18, DataType = "datetime", Format = "MM/dd/yyyy", IsFilterable = true },
              //  new ColumnMetadata { FieldName = "ApprovedDate", DisplayName = "Approved Date", Description = "Date of approval", DisplayOrder = 19, DataType = "datetime", Format = "MM/dd/yyyy", IsFilterable = true },
                new ColumnMetadata { FieldName = "FullyPaid", DisplayName = "Fully Paid Date", Description = "Date when fully paid", DisplayOrder = 20, DataType = "datetime", Format = "MM/dd/yyyy", IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "InitialCost", DisplayName = "Initial Cost", Description = "Initial cost of the project", DisplayOrder = 21, DataType = "decimal", Format = "C2", IsFilterable = true },
                new ColumnMetadata { FieldName = "TotalEstFee", DisplayName = "Total Est. Fee", Description = "Total estimated fee", DisplayOrder = 22, DataType = "decimal", Format = "C2", IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "FeeStatus", DisplayName = "Fee Status", Description = "Status of fee payment", DisplayOrder = 23, IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "ExistingDwellingUnits", DisplayName = "Existing Dwelling Units", Description = "Number of existing dwelling units", DisplayOrder = 24, IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "ProposedDwellingUnits", DisplayName = "Proposed Dwelling Units", Description = "Number of proposed dwelling units", DisplayOrder = 25, DataType = "number", IsFilterable = true },
                new ColumnMetadata { FieldName = "ExistingOccupancy", DisplayName = "Existing Occupancy", Description = "Existing occupancy classification", DisplayOrder = 26, IsFilterable = true, Visible = false, Width = 180 },
                new ColumnMetadata { FieldName = "ProposedOccupancy", DisplayName = "Proposed Occupancy", Description = "Proposed occupancy classification", DisplayOrder = 27, IsFilterable = true, Visible = false, Width = 180 },
                new ColumnMetadata { FieldName = "ExistingNoOfStories", DisplayName = "Existing # of Stories", Description = "Number of existing stories", DisplayOrder = 28, DataType = "number", IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "ProposedNoOfStories", DisplayName = "Proposed # of Stories", Description = "Number of proposed stories", DisplayOrder = 29, DataType = "number", IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "ExistingZoningSqft", DisplayName = "Existing Zoning Sqft", Description = "Existing zoning square footage", DisplayOrder = 30, DataType = "number", IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "ProposedZoningSqft", DisplayName = "Proposed Zoning Sqft", Description = "Proposed zoning square footage", DisplayOrder = 31, DataType = "number", IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "HorizontalEnlrgmt", DisplayName = "Horizontal Enlargement", Description = "Horizontal enlargement", DisplayOrder = 32, IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "VerticalEnlrgmt", DisplayName = "Vertical Enlargement", Description = "Vertical enlargement", DisplayOrder = 33, IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "EnlargementSqFootage", DisplayName = "Enlargement SQ Footage", Description = "Enlargement square footage", DisplayOrder = 34, DataType = "number", IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "OwnerType", DisplayName = "Owner Type", Description = "Type of owner", DisplayOrder = 35, IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "OwnerSFirstName", DisplayName = "Owner's First Name", Description = "First name of the owner", DisplayOrder = 36, IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "OwnerSLastName", DisplayName = "Owner's Last Name", Description = "Last name of the owner", DisplayOrder = 37, IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "OwnerSBusinessName", DisplayName = "Owner's Business Name", Description = "Business name of the owner", DisplayOrder = 38, IsFilterable = true, Visible = false, Width = 200 },
                new ColumnMetadata { FieldName = "OwnerSHouseNumber", DisplayName = "Owner's House Number", Description = "House number of the owner", DisplayOrder = 39, IsFilterable = true, Visible = false },
                new ColumnMetadata { FieldName = "OwnerSHouseStreetName", DisplayName = "Owner's House Street Name", Description = "Street name of the owner's house", DisplayOrder = 40, IsFilterable = true, Visible = false, Width = 200 },
                new ColumnMetadata { FieldName = "OwnerSPhone", DisplayName = "Owner's Phone #", Description = "Phone number of the owner", DisplayOrder = 41, DataType = "number", IsFilterable = true, Visible = false }
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
