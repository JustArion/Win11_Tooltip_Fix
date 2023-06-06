using System.Reflection;
using Serilog;
using Dawn.Apps.Tooltip_Fix;
using Dawn.Apps.Tooltip_Fix.Serilog.CustomEnrichers;
using Serilog.Settings.Configuration;

var loggingConfig = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", false, true)
    .Build();

/* Prevention of the following exception under Single-File Publish:
 * System.InvalidOperationException: No Serilog:Using configuration section is defined and no Serilog assemblies were found.
    This is most likely because the application is published as single-file.
 */
var options = new ConfigurationReaderOptions(
    typeof(ConsoleLoggerConfigurationExtensions).Assembly, 
    typeof(Serilog.Enrichers.ProcessNameEnricher).Assembly, 
    typeof(Serilog.AspNetCore.RequestLoggingOptions).Assembly);

Log.Logger = new LoggerConfiguration()
    .Enrich.WithClassName()
    .ReadFrom.Configuration(loggingConfig, options)
    .CreateLogger();


try
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(services => { services.AddHostedService<Worker>(); })
        .UseSerilog()
        .Build();

    await host.RunAsync();
}
catch (Exception e)
{
    Log.Fatal(e, "Fatal Error");
}
finally
{
    await Log.CloseAndFlushAsync();
}