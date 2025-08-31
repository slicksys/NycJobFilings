# NYC DOB Job Filings Explorer

A Blazor Server application for exploring NYC Department of Buildings Job Application Filings data with dynamic columns, smart filters, and progressive loading capabilities.

## Overview

This application demonstrates a data exploration UI for the NYC DOB Job Application Filings dataset from NYC Open Data. The application focuses on three main pillars:

1. **Dynamic Columns & Preferences**: Load columns from metadata, show/hide, reorder, pin, and persist user preferences.
2. **Filter Chips + Chart**: Add/remove filter chips for different fields, apply to both grid and chart visualizations.
3. **Background/Progressive Loading**: Show first ~1,000 rows immediately, then stream remaining data in batches with progress indicators and cancellation support.

## Architecture & Implementation Choices

### Data Access Layer

- Entity Framework Core with SQL Server for the database
- JobFilingsDbContext with appropriate entity configuration
- Domain model based on the NYC DOB Job Filings dataset structure
- Optimized indexes for filtering on common fields (LatestActionDate, Borough, JobType+JobStatus, InitialCost)

### Services

- **ColumnMetadataService**: Manages column metadata and user preferences
- **FilterService**: Builds dynamic LINQ expressions from filter conditions
- **JobFilingService**: Core data access for job filings
- **ProgressiveLoadingService**: Handles background data loading with progress tracking

### UI Components

- Blazor Server for the web application
- Components for job filings explorer, column preferences, and saved filters
- Filter chip UI for intuitive filter creation and management
- (DevExpress DxGrid would be used in a full implementation)

## Data Loading Strategy

1. Initial fast load of ~1,000 records
2. Background loading of remaining records in batches of 5,000
3. Progress tracking with cancellation support
4. Optimized SQL queries by selecting only visible columns when possible

## Future Enhancements

1. Complete DevExpress DxGrid integration (removed due to license requirements)
2. More sophisticated chart visualizations
3. Full-text search capabilities
4. Azure deployment with AppInsights monitoring
5. User authentication and personalization

## Setup Instructions

### Prerequisites

- .NET 7.0 SDK or newer
- SQL Server (LocalDB or Express)
- Visual Studio 2022 or VS Code with C# extension

### Database Setup

1. Modify the connection string in `appsettings.json` if needed
2. Run Entity Framework migrations to create the database:
   ```
   dotnet ef migrations add InitialCreate -p NycJobFilings.Data -s NycJobFilings.Web
   dotnet ef database update -p NycJobFilings.Data -s NycJobFilings.Web
   ```
3. Import data from the NYC Open Data API or CSV export (see Data Import section)

### Running the Application

1. Build the solution:
   ```
   dotnet build
   ```
2. Run the web application:
   ```
   dotnet run --project NycJobFilings.Web
   ```
3. Navigate to `https://localhost:5001` or `http://localhost:5000`

## Data Import

For development/demo purposes, we recommend importing a subset of the data (e.g., the last 12-24 months) to reach at least 100,000 rows. Options for importing:

1. **NYC Open Data API**: Use the [Socrata API](https://dev.socrata.com/foundry/data.cityofnewyork.us/ic3t-wcy2) with SoQL queries
2. **CSV Export**: Download a CSV export from the [NYC Open Data portal](https://data.cityofnewyork.us/Housing-Development/DOB-Job-Application-Filings/ic3t-wcy2/about_data)

A data import script is provided in the `Scripts` folder.

## Trade-offs and Decisions

- Chose SQL Server for its robustness and indexing capabilities
- Implemented dynamic filtering with LINQ Expression trees for flexibility
- Used background processing with System.Threading.Channels for progressive loading
- Metadata-driven UI to support customization without code changes

---

Created for the Blazor + DevExpress DOB Jobs Data Explorer take-home exercise.
