using AppKeeperService.Models;
using Microsoft.Extensions.Options;

namespace AppKeeperService;

public sealed class WindowsBackgroundService(ILogger<WindowsBackgroundService> logger, KeepItUp keepItUp, IOptions<CoreSettings> coreOptions) : BackgroundService
{
	private readonly KeepItUp keepItUp = keepItUp;
	private readonly CoreSettings coreSettings = coreOptions.Value;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				// some operation here
				await keepItUp.InspectStatus();

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
