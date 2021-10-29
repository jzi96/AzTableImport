using Microsoft.Extensions.Hosting;
using System.CommandLine;
using Microsoft.Azure.Cosmos.Table;
using System.Globalization;

namespace Janzi.Projects.AzTableImport;
internal class ConsoleHostedService : BackgroundService
{
    private readonly ILogger logger;
    private readonly IHostApplicationLifetime appLifetime;
    private readonly IConfiguration config;
    private int exitCode = -1;
    internal static string[] Arguments { get; set; }
    public ConsoleHostedService(
        ILogger<ConsoleHostedService> logger,
        IHostApplicationLifetime appLifetime,
        IConfiguration config
        )
    {
        this.logger = logger;
        this.appLifetime = appLifetime;
        this.config = config;
    }

    // https://github.com/dotnet/command-line-api
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            Environment.ExitCode = exitCode;
        });
        var args = Arguments;
        logger.LogDebug("Starting with arguments: {args}", string.Join(" ", args));
        var fileOpt = new Option<FileInfo[]>("--import-file")
        {
            AllowMultipleArgumentsPerToken = true,
            IsRequired = true
        };
        fileOpt.AddAlias("-f");
        RootCommand command = new RootCommand
        {
            fileOpt,
            new Option<string>("--connection", () => config.GetConnectionString("storage"))
        };

        command.TreatUnmatchedTokensAsErrors = true;
        command.Description = "Application to import csv into Azure table storage";
        // command.AddCommand(new Command("import"))
        command.Handler = System.CommandLine.Invocation.CommandHandler.Create<string, FileInfo[]>(Import);

        return command.InvokeAsync(args).ContinueWith((result) =>
        {
            exitCode = result.Result;
            appLifetime.StopApplication();
        }, stoppingToken);
    }
    public void Import(string connection, FileInfo[] importFile)
    {
        this.logger.LogInformation("connection: {con}", connection);
        var account = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(connection);
        var tableClient = account.CreateCloudTableClient();
        foreach (var file in importFile)
        {
            this.logger.LogDebug("File: {file}", file);
            if (!file.Exists)
            {
                logger.LogWarning("Files does not exists. File={file}", file);
                continue;
            }
            int line  = 0;
            using (var csvReader = new StreamReader(file.FullName))
            using (var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader))
            {
                if (parser.EndOfData)
                    return;
                logger.LogInformation("[{line}] Processing line", line++);
                string[] headerFields = parser.ReadFields();
                var tableRef = tableClient.GetTableReference(Path.GetFileNameWithoutExtension(file.Name));
                tableRef.CreateIfNotExists();

                ProcessEntries(ref line, parser, headerFields, tableRef);
            }
        }
    }

    private void ProcessEntries(ref int line, NotVisualBasic.FileIO.CsvTextFieldParser parser, string[] headerFields, CloudTable tableRef)
    {
        while (!parser.EndOfData)
        {
            logger.LogInformation("[{line}] Processing line", line++);
            string[] fields = parser.ReadFields();
            var fieldCount = Math.Min(headerFields.Length, fields.Length);
            DynamicTableEntity de = new DynamicTableEntity(fields[0], fields[1]);
            //2 timestamp
            for (int i = 3; i < fieldCount; i++)
            {
                string header = headerFields[i];
                string textVal = fields[i++];
                string type = fields[i];
                de[header] = type switch
                {
                    //
                    "Edm.Int32" => new EntityProperty(Convert.ToInt32(textVal, CultureInfo.InvariantCulture)),
                    "Edm.Boolean" => new EntityProperty(Convert.ToBoolean(textVal, CultureInfo.InvariantCulture)),
                    "Edm.Double" => new EntityProperty(Convert.ToDouble(textVal, CultureInfo.InvariantCulture)),
                    "Edm.DateTime" => new EntityProperty(Convert.ToDateTime(textVal, CultureInfo.InvariantCulture)),
                    _ => new EntityProperty(textVal),
                };
            }
            //column, type column
            TableOperation operation = TableOperation.InsertOrReplace(de);
            logger.LogInformation("[{line}] Storing data", line);
            tableRef.Execute(operation);
        }
    }
}