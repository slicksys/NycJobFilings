# NYC DOB Job Filings Explorer - Quick Start Guide

This guide will help you get started with the NYC DOB Job Filings Explorer application.

## Setup

1. Clone the repository
2. Ensure you have .NET 7.0 SDK or newer installed
3. Update the connection string in `NycJobFilings.Web/appsettings.json` if needed
4. Initialize the database with test data:

```bash
dotnet run --project NycJobFilings.Data/Scripts/InitializeDatabase.cs test 100000
```

This will generate 100,000 sample job filing records for development purposes.

## Running the Application

```bash
dotnet run --project NycJobFilings.Web
```

Navigate to `https://localhost:5001` or `http://localhost:5000` in your browser.

## Using the Explorer

### Job Filings Explorer

The main screen shows job filings with:

1. **Filter Chips**: Add filters by clicking "+ Add Filter" or remove existing filters
2. **Data Grid**: Displays job filings based on the current filters
3. **Chart View**: Shows trends based on the current filters

By default, the application shows filings from the last 12 months.

### Column Preferences

Customize the columns displayed in the grid:

1. Click "Column Settings" in the navigation menu
2. Toggle visibility using checkboxes
3. Change display order with the order input
4. Adjust column width and pinning options
5. Click "Save Preferences" to store your settings

### Saved Filters

Manage your saved filter sets:

1. Create filters in the Job Filings Explorer
2. Click "Save Filter" to name and save your filter set
3. Access saved filters from the "Saved Filters" navigation item
4. Apply or delete saved filters as needed

## Progressive Loading

When working with large result sets:

1. The first ~1,000 records are loaded immediately
2. Additional records are loaded in the background
3. Progress is shown with a progress bar
4. Click "Cancel" to stop loading more records if needed

## Performance Tips

1. Use specific filters to reduce the result set size
2. Hide unused columns to improve loading performance
3. Save commonly used filter combinations
