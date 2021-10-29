// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Janzi.Projects.AzTableImport;

await Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<ConsoleHostedService>();
                ConsoleHostedService.Arguments = args;
            })
            .RunConsoleAsync().ConfigureAwait(false);
