using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NycJobFilings.Data.Models;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NycJobFilings.Data.Scripts
{
    /// <summary>
    /// Utility class for importing NYC DOB Job Filings data from NYC Open Data
    /// </summary>
    public class DataImporter
    {
        private readonly JobFilingsDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DataImporter> _logger;

        public DataImporter(JobFilingsDbContext dbContext, HttpClient httpClient, ILogger<DataImporter> logger)
        {
            _dbContext = dbContext;
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Import data from NYC Open Data API using SoQL query
        /// </summary>
        public async Task ImportFromApiAsync(int limit = 10000, int? monthsBack = 12)
        {
            try
            {
                string soqlQuery = BuildSoqlQuery(limit, monthsBack);
                string apiUrl = $"https://data.cityofnewyork.us/resource/ic3t-wcy2.json?{soqlQuery}";
                
                _logger.LogInformation("Fetching data from NYC Open Data API: {Url}", apiUrl);
                
                // Fetch data from API
                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var filings = await response.Content.ReadFromJsonAsync<List<JobFilingApiModel>>(options);
                
                if (filings == null || !filings.Any())
                {
                    _logger.LogWarning("No data retrieved from API");
                    return;
                }
                
                _logger.LogInformation("Retrieved {Count} records from API", filings.Count);
                
                // Convert API models to entity models and save to database
                var entities = filings.Select(MapApiModelToEntity).ToList();
                await SaveToDatabase(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing data from NYC Open Data API");
                throw;
            }
        }

        /// <summary>
        /// Import data from CSV file
        /// </summary>
        public async Task ImportFromCsvAsync(string csvFilePath)
        {
            try
            {
                _logger.LogInformation("Importing data from CSV file: {FilePath}", csvFilePath);
                
                // Read CSV file
                using var reader = new StreamReader(csvFilePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null
                });
                
                // Map CSV records to entity models
                var records = csv.GetRecords<JobFilingCsvModel>().ToList();
                _logger.LogInformation("Read {Count} records from CSV file", records.Count);
                
                // Convert CSV models to entity models
                var entities = records.Select(MapCsvModelToEntity).ToList();
                await SaveToDatabase(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing data from CSV file");
                throw;
            }
        }

        /// <summary>
        /// Generate test data for development
        /// </summary>
        public async Task GenerateTestDataAsync(int count = 10000)
        {
            try
            {
                _logger.LogInformation("Generating {Count} test records", count);
                
                var entities = new List<JobFiling>();
                var random = new Random(42); // Fixed seed for reproducibility
                var boroughs = new[] { "MANHATTAN", "BROOKLYN", "QUEENS", "BRONX", "STATEN ISLAND" };
                var jobTypes = new[] { "NB", "A1", "A2", "DM", "PA" };
                var jobStatuses = new[] { "APPROVED", "PENDING", "REJECTED", "IN PROCESS", "COMPLETED" };
                
                var baseDate = DateTime.Now.AddYears(-1);
                
                for (int i = 0; i < count; i++)
                {
                    var entity = new JobFiling
                    {
                        JobS1No = $"TEST{i:D6}",
                        Borough = boroughs[random.Next(boroughs.Length)],
                        HouseNo = random.Next(1, 999).ToString(),
                        StreetName = $"TEST STREET {random.Next(1, 100)}",
                        Block = random.Next(1, 9999).ToString(),
                        Lot = random.Next(1, 999).ToString(),
                        ZipCode = $"{random.Next(10000, 99999)}",
                        BuildingType = random.Next(1, 10).ToString(),
                        JobType = jobTypes[random.Next(jobTypes.Length)],
                        JobStatus = jobStatuses[random.Next(jobStatuses.Length)],
                        JobStatusDescription = $"Test status {i}",
                        LatestActionDate = baseDate.AddDays(random.Next(365)),
                        FilingDate = baseDate.AddDays(-random.Next(365)),
                        InitialCost = (decimal)(random.NextDouble() * 1000000),
                        TotalEstimatedFee = (decimal)(random.NextDouble() * 50000),
                        ExistingDwellingUnits = random.Next(1, 200),
                        ProposedDwellingUnits = random.Next(1, 300)
                    };
                    
                    entities.Add(entity);
                    
                    // Save in batches to avoid memory issues
                    if (i % 1000 == 0 && i > 0)
                    {
                        await SaveToDatabase(entities);
                        entities.Clear();
                    }
                }
                
                // Save any remaining entities
                if (entities.Any())
                {
                    await SaveToDatabase(entities);
                }
                
                _logger.LogInformation("Generated and saved {Count} test records", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test data");
                throw;
            }
        }

        private string BuildSoqlQuery(int limit, int? monthsBack)
        {
            var query = new List<string>();
            
            // Add limit
            query.Add($"$limit={limit}");
            
            // Add time filter if specified
            if (monthsBack.HasValue)
            {
                var cutoffDate = DateTime.Now.AddMonths(-monthsBack.Value).ToString("yyyy-MM-dd");
                query.Add($"$where=latest_action_date > '{cutoffDate}'");
            }
            
            return string.Join("&", query);
        }

        private JobFiling MapApiModelToEntity(JobFilingApiModel apiModel)
        {
            return new JobFiling
            {
                JobS1No = apiModel.JobS1No ?? throw new InvalidDataException("Job S1 No is required"),
                Borough = apiModel.Borough,
                HouseNo = apiModel.HouseNo,
                StreetName = apiModel.StreetName,
                Block = apiModel.Block,
                Lot = apiModel.Lot,
                ZipCode = apiModel.ZipCode,
                BuildingType = apiModel.BuildingType,
                JobType = apiModel.JobType,
                JobStatus = apiModel.JobStatus,
                JobStatusDescription = apiModel.JobStatusDescrp,
                LatestActionDate = ParseDateTime(apiModel.LatestActionDate),
                FilingDate = ParseDateTime(apiModel.FilingDate),
                ApprovedDate = ParseDateTime(apiModel.ApprovedDate),
                FullyPaid = apiModel.FullyPaid,
                InitialCost = ParseDecimal(apiModel.InitialCost),
                TotalEstimatedFee = ParseDecimal(apiModel.TotalEstFee),
                FeeStatus = apiModel.FeeStatus,
                ExistingDwellingUnits = ParseInt(apiModel.ExistingDwellingUnits),
                ProposedDwellingUnits = ParseInt(apiModel.ProposedDwellingUnits),
                ExistingOccupancy = apiModel.ExistingOccupancy,
                ProposedOccupancy = apiModel.ProposedOccupancy,
                ExistingStories = ParseInt(apiModel.ExistingStories),
                ProposedStories = ParseInt(apiModel.ProposedStories),
                ExistingZoningSquareFeet = ParseDecimal(apiModel.ExistingZoningSquareFeet),
                ProposedZoningSquareFeet = ParseDecimal(apiModel.ProposedZoningSquareFeet),
                HorizontalEnlargement = ParseDecimal(apiModel.HorizontalEnlargement),
                VerticalEnlargement = ParseDecimal(apiModel.VerticalEnlargement),
                EnlargementSquareFeet = ParseDecimal(apiModel.EnlargementSquareFeet),
                OwnerType = apiModel.OwnerType,
                OwnerName = apiModel.OwnerName,
                OwnerBusiness = apiModel.OwnerBusiness,
                OwnerHouseStreet = apiModel.OwnerHouseStreet,
                CityStateZip = apiModel.CityStateZip
            };
        }

        private JobFiling MapCsvModelToEntity(JobFilingCsvModel csvModel)
        {
            return new JobFiling
            {
                JobS1No = csvModel.JobS1No ?? throw new InvalidDataException("Job S1 No is required"),
                Borough = csvModel.Borough,
                HouseNo = csvModel.HouseNo,
                StreetName = csvModel.StreetName,
                Block = csvModel.Block,
                Lot = csvModel.Lot,
                ZipCode = csvModel.ZipCode,
                BuildingType = csvModel.BuildingType,
                JobType = csvModel.JobType,
                JobStatus = csvModel.JobStatus,
                JobStatusDescription = csvModel.JobStatusDescription,
                LatestActionDate = ParseDateTime(csvModel.LatestActionDate),
                FilingDate = ParseDateTime(csvModel.FilingDate),
                ApprovedDate = ParseDateTime(csvModel.ApprovedDate),
                FullyPaid = csvModel.FullyPaid,
                InitialCost = ParseDecimal(csvModel.InitialCost),
                TotalEstimatedFee = ParseDecimal(csvModel.TotalEstimatedFee),
                FeeStatus = csvModel.FeeStatus,
                ExistingDwellingUnits = ParseInt(csvModel.ExistingDwellingUnits),
                ProposedDwellingUnits = ParseInt(csvModel.ProposedDwellingUnits),
                ExistingOccupancy = csvModel.ExistingOccupancy,
                ProposedOccupancy = csvModel.ProposedOccupancy,
                ExistingStories = ParseInt(csvModel.ExistingStories),
                ProposedStories = ParseInt(csvModel.ProposedStories),
                ExistingZoningSquareFeet = ParseDecimal(csvModel.ExistingZoningSquareFeet),
                ProposedZoningSquareFeet = ParseDecimal(csvModel.ProposedZoningSquareFeet),
                HorizontalEnlargement = ParseDecimal(csvModel.HorizontalEnlargement),
                VerticalEnlargement = ParseDecimal(csvModel.VerticalEnlargement),
                EnlargementSquareFeet = ParseDecimal(csvModel.EnlargementSquareFeet),
                OwnerType = csvModel.OwnerType,
                OwnerName = csvModel.OwnerName,
                OwnerBusiness = csvModel.OwnerBusiness,
                OwnerHouseStreet = csvModel.OwnerHouseStreet,
                CityStateZip = csvModel.CityStateZip
            };
        }

        private async Task SaveToDatabase(List<JobFiling> entities)
        {
            // Check for existing records to avoid duplicates
            var existingIds = await _dbContext.JobFilings
                .Where(jf => entities.Select(e => e.JobS1No).Contains(jf.JobS1No))
                .Select(jf => jf.JobS1No)
                .ToListAsync();
            
            var newEntities = entities.Where(e => !existingIds.Contains(e.JobS1No)).ToList();
            
            if (!newEntities.Any())
            {
                _logger.LogInformation("No new entities to save");
                return;
            }
            
            _logger.LogInformation("Saving {Count} new entities to database", newEntities.Count);
            await _dbContext.JobFilings.AddRangeAsync(newEntities);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Saved {Count} entities to database", newEntities.Count);
        }

        private static DateTime? ParseDateTime(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            
            return DateTime.TryParse(value, out var result) ? result : null;
        }

        private static decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            
            return decimal.TryParse(value, out var result) ? result : null;
        }

        private static int? ParseInt(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            
            return int.TryParse(value, out var result) ? result : null;
        }
    }

    // API model for deserializing JSON from NYC Open Data
    public class JobFilingApiModel
    {
        [JsonPropertyName("job_s1_no")]
        public string? JobS1No { get; set; }

        [JsonPropertyName("borough")]
        public string? Borough { get; set; }

        [JsonPropertyName("house_no")]
        public string? HouseNo { get; set; }

        [JsonPropertyName("street_name")]
        public string? StreetName { get; set; }

        [JsonPropertyName("block")]
        public string? Block { get; set; }

        [JsonPropertyName("lot")]
        public string? Lot { get; set; }

        [JsonPropertyName("zip_code")]
        public string? ZipCode { get; set; }

        [JsonPropertyName("bldg_type")]
        public string? BuildingType { get; set; }

        [JsonPropertyName("job_type")]
        public string? JobType { get; set; }

        [JsonPropertyName("job_status")]
        public string? JobStatus { get; set; }

        [JsonPropertyName("job_status_descrp")]
        public string? JobStatusDescrp { get; set; }

        [JsonPropertyName("latest_action_date")]
        public string? LatestActionDate { get; set; }

        [JsonPropertyName("filing_date")]
        public string? FilingDate { get; set; }

        [JsonPropertyName("approved_date")]
        public string? ApprovedDate { get; set; }

        [JsonPropertyName("fully_paid")]
        public string? FullyPaid { get; set; }

        [JsonPropertyName("initial_cost")]
        public string? InitialCost { get; set; }

        [JsonPropertyName("total_est_fee")]
        public string? TotalEstFee { get; set; }

        [JsonPropertyName("fee_status")]
        public string? FeeStatus { get; set; }

        [JsonPropertyName("existing_dwelling_units")]
        public string? ExistingDwellingUnits { get; set; }

        [JsonPropertyName("proposed_dwelling_units")]
        public string? ProposedDwellingUnits { get; set; }

        [JsonPropertyName("existing_occupancy")]
        public string? ExistingOccupancy { get; set; }

        [JsonPropertyName("proposed_occupancy")]
        public string? ProposedOccupancy { get; set; }

        [JsonPropertyName("existing_stories")]
        public string? ExistingStories { get; set; }

        [JsonPropertyName("proposed_stories")]
        public string? ProposedStories { get; set; }

        [JsonPropertyName("existing_zoning_sqft")]
        public string? ExistingZoningSquareFeet { get; set; }

        [JsonPropertyName("proposed_zoning_sqft")]
        public string? ProposedZoningSquareFeet { get; set; }

        [JsonPropertyName("horizontal_enlrgmt")]
        public string? HorizontalEnlargement { get; set; }

        [JsonPropertyName("vertical_enlrgmt")]
        public string? VerticalEnlargement { get; set; }

        [JsonPropertyName("enlargement_sqft")]
        public string? EnlargementSquareFeet { get; set; }

        [JsonPropertyName("owner_type")]
        public string? OwnerType { get; set; }

        [JsonPropertyName("owner_name")]
        public string? OwnerName { get; set; }

        [JsonPropertyName("owner_business")]
        public string? OwnerBusiness { get; set; }

        [JsonPropertyName("owner_house_street")]
        public string? OwnerHouseStreet { get; set; }

        [JsonPropertyName("city_state_zip")]
        public string? CityStateZip { get; set; }
    }

    // CSV model for reading from CSV files
    public class JobFilingCsvModel
    {
        public string? JobS1No { get; set; }
        public string? Borough { get; set; }
        public string? HouseNo { get; set; }
        public string? StreetName { get; set; }
        public string? Block { get; set; }
        public string? Lot { get; set; }
        public string? ZipCode { get; set; }
        public string? BuildingType { get; set; }
        public string? JobType { get; set; }
        public string? JobStatus { get; set; }
        public string? JobStatusDescription { get; set; }
        public string? LatestActionDate { get; set; }
        public string? FilingDate { get; set; }
        public string? ApprovedDate { get; set; }
        public string? FullyPaid { get; set; }
        public string? InitialCost { get; set; }
        public string? TotalEstimatedFee { get; set; }
        public string? FeeStatus { get; set; }
        public string? ExistingDwellingUnits { get; set; }
        public string? ProposedDwellingUnits { get; set; }
        public string? ExistingOccupancy { get; set; }
        public string? ProposedOccupancy { get; set; }
        public string? ExistingStories { get; set; }
        public string? ProposedStories { get; set; }
        public string? ExistingZoningSquareFeet { get; set; }
        public string? ProposedZoningSquareFeet { get; set; }
        public string? HorizontalEnlargement { get; set; }
        public string? VerticalEnlargement { get; set; }
        public string? EnlargementSquareFeet { get; set; }
        public string? OwnerType { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerBusiness { get; set; }
        public string? OwnerHouseStreet { get; set; }
        public string? CityStateZip { get; set; }
    }
}
