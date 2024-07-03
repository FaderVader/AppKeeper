using AppKeeperService.Models;
using AppKeeperService.Workers;
using Microsoft.Extensions.Options;

namespace AppKeeperService.Service;

public sealed class WindowsBackgroundService(ILogger<WindowsBackgroundService> logger, IAppMonitor monitor, IOptions<CoreSettings> coreOptions) : BackgroundService
{
    private readonly IAppMonitor monitor = monitor;
    private readonly CoreSettings coreSettings = coreOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await monitor.InspectStatus();

                await Task.Delay(coreSettings.RecheckStatusIntervalInSecs * 1000);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("The process was stopped");
        }

        catch (Exception e)
        {
            logger.LogError(e, "{Message}", e.Message);
            Environment.Exit(1);
        }
    }
}
