# AzTableImport

The application loads the csv file and will store all entries into an
Azure Table Storage.
The csv file should have been generated with 
[Azure Storage Explorer](https://docs.microsoft.com/en-us/azure/vs-azure-tools-storage-manage-with-storage-explorer?tabs=windows).
## Required to build

* [.NET 6 (C# 10)](https://dotnet.microsoft.com/download/dotnet/6.0)

## Build application

* build app  `dotnet build -c Release src/AzTableImport/AzTableImport.csproj`

* run `dotnet run -c Release src/AzTableImport/AzTableImport.csproj --file-import myazuretableexport.csv`

The connection string for the storage account can be configured in `appsettings.json` or passed
via command line, see command help for details.