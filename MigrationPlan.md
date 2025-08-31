# NYC DOB Job Filings Explorer - Migration Plan

This document outlines the plan for setting up the database and migrating data for the NYC DOB Job Filings Explorer application.

## Database Setup

1. **Create Initial Migration**:
   ```bash
   dotnet ef migrations add InitialCreate --project NycJobFilings.Data --startup-project NycJobFilings.Web
   ```

2. **Apply Migration to Create Database**:
   ```bash
   dotnet ef database update --project NycJobFilings.Data --startup-project NycJobFilings.Web
   ```

## Data Import Options

Choose one of the following methods to import data:

### Option 1: Import from NYC Open Data API

This method fetches recent data directly from the NYC Open Data API:

```bash
dotnet run --project NycJobFilings.Data/Scripts/DataImporter.cs api 100000 24
```

Parameters:
- `api`: Specifies API import method
- `100000`: Maximum number of records to fetch (adjust as needed)
- `24`: Fetch records from the last 24 months

### Option 2: Import from CSV File

If you have a CSV export from the NYC Open Data portal:

```bash
dotnet run --project NycJobFilings.Data/Scripts/DataImporter.cs csv "/path/to/DOB_Job_Application_Filings.csv"
```

Parameters:
- `csv`: Specifies CSV import method
- File path to the downloaded CSV file

### Option 3: Generate Test Data

For development and testing purposes:

```bash
dotnet run --project NycJobFilings.Data/Scripts/DataImporter.cs test 100000
```

Parameters:
- `test`: Specifies test data generation
- `100000`: Number of test records to generate

## Production Considerations

For a production deployment:

1. **Database Optimization**:
   - Consider partitioning by year/month for better performance on large datasets
   - Add appropriate indexes as identified from query patterns

2. **Data Refresh Strategy**:
   - Set up a daily/weekly job to import new records
   - Consider an incremental import approach based on latest_action_date

3. **Performance Tuning**:
   - Monitor query performance and optimize problem areas
   - Consider caching frequently accessed data
   - Implement database level paging for large result sets
