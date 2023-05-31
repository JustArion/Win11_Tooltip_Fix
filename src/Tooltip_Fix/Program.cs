using System.Reflection;
using Serilog;
using Dawn.Apps.Tooltip_Fix;
using Dawn.Apps.Tooltip_Fix.Serilog.CustomEnrichers;

var loggingConfig = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", false, true)
    .Build();



Log.Logger = new LoggerConfiguration()
    .Enrich.WithClassName()
    .ReadFrom.Configuration(loggingConfig)
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