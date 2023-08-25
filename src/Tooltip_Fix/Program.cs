using System.Reflection;
using Serilog;
using Dawn.Apps.Tooltip_Fix;
using Dawn.Apps.Tooltip_Fix.Serilog.CustomEnrichers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog.Settings.Configuration;
using Vanara.PInvoke;

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
    .WriteTo.Seq("http://localhost:9999")
    .CreateLogger();

Log.Information("Starting {AssemblyName} v{AssemblyVersion}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version);

try
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(services =>
        {
            services.AddHostedService<Worker>();
        })
        .UseWindowsService()
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
