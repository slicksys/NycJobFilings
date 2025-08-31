using NycJobFilings.Data.Scripts;

// Display a banner
Console.WriteLine("=========================================");
Console.WriteLine("NYC Job Filings Database Initializer Tool");
Console.WriteLine("=========================================");
Console.WriteLine();

// Run the database initialization script
await InitializeDatabaseScript.RunAsync(args);
