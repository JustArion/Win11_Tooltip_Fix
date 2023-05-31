namespace Dawn.Apps.Tooltip_Fix;

using global::Serilog;
using global::Serilog.Core;

public class Worker : BackgroundService
{
    private const string ServiceName = "Tooltip Fix Service";

    private WorkerService _CurrentService;
    
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Logger.Information("{ServiceName} Started", ServiceName);

        try
        {
            _CurrentService?.EnsureDisposed();
            _CurrentService = new WorkerService();
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Error while initializing {ServiceName}", ServiceName);
        }

        
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        => await Task.Yield();

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _CurrentService?.EnsureDisposed();
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Error while disposing {ServiceName}", ServiceName);
        }
        finally
        {
            Log.Logger.Information("{ServiceName} Stopped", ServiceName);
            Log.CloseAndFlush();
        }
        
        return base.StopAsync(cancellationToken);
    }
}