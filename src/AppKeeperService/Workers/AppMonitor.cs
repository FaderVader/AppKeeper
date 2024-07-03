using AppKeeperService.Models;
using AppKeeperService.Utils;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace AppKeeperService.Workers;
public class AppMonitor : IAppMonitor
{
    private readonly ILogger<AppMonitor> logger;
    private readonly CoreSettings coreSettings;

    public AppMonitor(ILogger<AppMonitor> logger, IOptions<CoreSettings> coreOptions)
    {
        this.logger = logger;
        coreSettings = coreOptions.Value;
    }

    public async Task InspectStatus()
    {
        try
        {
            coreSettings.ApplicationList.ForEach(async app =>
            {
                logger.LogInformation("Now checking status of {appName}", app);

                var targetProcess = Process.GetProcessesByName(app.DisplayName).FirstOrDefault();

                if (targetProcess is null)
                {
                    logger.LogWarning("Process named {appName} was not found among active processes.", app.DisplayName);
                    await StartApp(app.PathToExe);
                    logger.LogInformation("Started app {appName} from {path}.", app.DisplayName, app.PathToExe);
                    return;
                }

                if (!targetProcess.Responding)
                {
                    logger.LogWarning("Process named {appName} is not responding.", app.DisplayName);
                    targetProcess.Kill();
                    await Task.Delay(coreSettings.TimeoutAfterKillInSecs * 1000);
                    await StartApp(app.PathToExe);
                    logger.LogInformation("Started app {appName} from {path}.", app.DisplayName, app.PathToExe);
                    return;
                }

                if (targetProcess.HasExited)
                {
                    logger.LogWarning("Process named {appName} has been terminated.", app.DisplayName);
                    targetProcess.Kill();
                    await Task.Delay(coreSettings.TimeoutAfterKillInSecs * 1000);
                    await StartApp(app.PathToExe);
                    logger.LogInformation("Started app {appName} from {path}.", app.DisplayName, app.PathToExe);
                    return;
                }

                logger.LogInformation("Found {appName}/{processName} as running OK.", app.DisplayName, targetProcess.ProcessName);
            });
        }
        catch (Exception e)
        {
            logger.LogError("Error while checking application-state: {msg}", e.Message);
        }
    }

    public async Task<bool> StartApp(string processName, string args = "")
    {
        // ApplicationLoaderHelper requires app-name and args as single arg, so we concatenate
        var _processAndArgs = $"{processName} {args}";
        logger.LogInformation("StartProcessAsUser: args: {args}", _processAndArgs);

        (bool processStartedOk, int processId) _result = (false, 0);

        try
        {
            _result = await Task.Run(() => ApplicationLoaderHelper.StartProcessAndBypassUAC(_processAndArgs, logger));
        }
        catch (Exception e)
        {
            logger.LogError("StartProcessAsUser - failed to start application from session0. Error:\n{message}", e.Message);
            throw;
        }

        logger.LogInformation("StartProcessAsUser - Started process: {info}", _result.processId);
        return _result.processStartedOk;
    }
}
