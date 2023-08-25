namespace Dawn.Apps.Tooltip_Fix;

using global::Serilog;

public class Worker : BackgroundService
{
    private const string ServiceName = "Tooltip Fix Service";

    private MessageRunner _CurrentService;
    
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Logger.Information("{ServiceName} Started", ServiceName);
        

        try
        {
            _CurrentService?.EnsureDisposed();
            _CurrentService = new MessageRunner();
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Error while initializing {ServiceName}", ServiceName);
        }

        
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tcs = new TaskCompletionSource();
        stoppingToken.Register(s => ((TaskCompletionSource)s).SetResult(), tcs);
        await tcs.Task;
    }

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