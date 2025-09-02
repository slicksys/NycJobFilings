using NycJobFilings.Data.Models;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace NycJobFilings.Data.Services
{
    public class FilterCondition
    {
        public string FieldName { get; set; } = default!;
        public string Operator { get; set; } = default!;
        public object? Value { get; set; }
        public string? DisplayText { get; set; }
    }

    public class FilterSet
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public List<FilterCondition> Conditions { get; set; } = new List<FilterCondition>();
    }

    public class FilterService
    {
        private readonly string _savedFiltersDirectory;
        private readonly ILogger<FilterService> _logger;

        public FilterService(string savedFiltersDirectory, ILogger<FilterService> logger)
        {
            _savedFiltersDirectory = savedFiltersDirectory;
            _logger = logger;

            // Ensure directory exists
            if (!Directory.Exists(savedFiltersDirectory))
            {
                Directory.CreateDirectory(savedFiltersDirectory);
            }
        }

        /// <summary>
        /// Builds a LINQ expression from filter conditions
        /// </summary>
        public Expression<Func<JobFiling, bool>> BuildFilterExpression(List<FilterCondition> conditions)
        {
            if (conditions == null || !conditions.Any())
            {
                // Return a "true" expression to select all records
                return j => true;
            }

            // Start with a parameter expression for JobFiling
            var parameter = Expression.Parameter(typeof(JobFiling), "j");
            
            // Combine all conditions with AND
            Expression? combinedExpression = null;

            foreach (var condition in conditions)
            {
                var singleCondition = BuildSingleCondition(parameter, condition);
                
                combinedExpression = combinedExpression == null
                    ? singleCondition
                    : Expression.AndAlso(combinedExpression, singleCondition);
            }

            // If no valid conditions, return a "true" expression
            if (combinedExpression == null)
            {
                combinedExpression = Expression.Constant(true);
            }

            // Create and return the lambda expression
            return Expression.Lambda<Func<JobFiling, bool>>(combinedExpression, parameter);
        }

        private Expression BuildSingleCondition(ParameterExpression parameter, FilterCondition condition)
        {
            // Get property from the entity
            var property = Expression.Property(parameter, condition.FieldName);
            var propertyType = ((PropertyInfo)property.Member).PropertyType;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            
            // Convert value to the correct type
            object? typedValue = null;
            try
            {
                if (condition.Value != null)
                {
                    if (underlyingType == typeof(DateTime))
                    {
                        typedValue = condition.Value is DateTime ? condition.Value : DateTime.Parse(condition.Value.ToString()!);
                    }
                    else if (underlyingType == typeof(decimal) || underlyingType == typeof(double))
                    {
                        typedValue = Convert.ToDecimal(condition.Value);
                    }
                    else if (underlyingType == typeof(int))
                    {
                        typedValue = Convert.ToInt32(condition.Value);
                    }
                    else
                    {
                        typedValue = Convert.ChangeType(condition.Value, underlyingType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting filter value {Value} to type {Type}", condition.Value, underlyingType.Name);
                // Return a "false" expression for invalid conversions
                return Expression.Constant(false);
            }

            // Create constant expression with the value
            var valueExpression = Expression.Constant(
                typedValue, 
                Nullable.GetUnderlyingType(propertyType) != null ? typeof(Nullable<>).MakeGenericType(underlyingType) : underlyingType);

            // Build the expression based on operator
            Expression comparison;
            switch (condition.Operator.ToLowerInvariant())
            {
                case "eq":
                case "=":
                case "==":
                    comparison = Expression.Equal(property, valueExpression);
                    break;
                    
                case "gt":
                case ">":
                    comparison = Expression.GreaterThan(property, valueExpression);
                    break;
                    
                case "ge":
                case ">=":
                    comparison = Expression.GreaterThanOrEqual(property, valueExpression);
                    break;
                    
                case "lt":
                case "<":
                    comparison = Expression.LessThan(property, valueExpression);
                    break;
                    
                case "le":
                case "<=":
                    comparison = Expression.LessThanOrEqual(property, valueExpression);
                    break;
                    
                case "contains":
                    if (underlyingType != typeof(string))
                    {
                        _logger.LogError("Contains operator only applies to strings, but property {PropertyName} is {PropertyType}", 
                            condition.FieldName, underlyingType.Name);
                        return Expression.Constant(false);
                    }
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    comparison = Expression.Call(property, containsMethod!, valueExpression);
                    break;
                    
                default:
                    _logger.LogError("Unknown operator: {Operator}", condition.Operator);
                    return Expression.Constant(false);
            }

            // Handle null values in the database
            if (Nullable.GetUnderlyingType(propertyType) != null)
            {
                var nullConstant = Expression.Constant(null, propertyType);
                var notNull = Expression.NotEqual(property, nullConstant);
                return Expression.AndAlso(notNull, comparison);
            }

            return comparison;
        }

        /// <summary>
        /// Saves a filter set for a user
        /// </summary>
        public async Task SaveFilterSetAsync(string userId, FilterSet filterSet)
        {
            if (string.IsNullOrEmpty(filterSet.Name))
            {
                throw new ArgumentException("Filter set must have a name");
            }

            filterSet.Id = filterSet.Id ?? Guid.NewGuid().ToString();
            
            var filePath = Path.Combine(_savedFiltersDirectory, $"{userId}-{filterSet.Id}.json");
            await File.WriteAllTextAsync(filePath, System.Text.Json.JsonSerializer.Serialize(filterSet));
        }

        /// <summary>
        /// Gets all saved filter sets for a user
        /// </summary>
        public async Task<List<FilterSet>> GetSavedFilterSetsAsync(string userId)
        {
            var result = new List<FilterSet>();
            var directory = new DirectoryInfo(_savedFiltersDirectory);
            
            if (!directory.Exists)
            {
                return result;
            }

            foreach (var file in directory.GetFiles($"{userId}-*.json"))
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file.FullName);
                    var filterSet = System.Text.Json.JsonSerializer.Deserialize<FilterSet>(content);
                    if (filterSet != null)
                    {
                        result.Add(filterSet);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading saved filter from {FilePath}", file.FullName);
                }
            }

            return result;
        }

        /// <summary>
        /// Deletes a saved filter set
        /// </summary>
        public Task DeleteFilterSetAsync(string userId, string filterId)
        {
            var filePath = Path.Combine(_savedFiltersDirectory, $"{userId}-{filterId}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            return Task.CompletedTask;
        }
    }

    // Extension for DI setup
    public static class FilterServiceExtensions
    {
        public static IServiceCollection AddFilterService(this IServiceCollection services, IConfiguration configuration)
        {
            var savedFiltersDir = configuration["Filters:SavedFiltersDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "Data", "SavedFilters");

            services.AddSingleton<FilterService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<FilterService>>();
                return new FilterService(savedFiltersDir, logger);
            });

            return services;
        }
    }
}
