using AppKeeperService.Models;
using AppKeeperService.Utils;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace AppKeeperService;
public class KeepItUp
{
    private readonly ILogger<KeepItUp> logger;
    private readonly CoreSettings coreSettings;

    public KeepItUp(ILogger<KeepItUp> logger, IOptions<CoreSettings> coreOptions)
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

                var processOfInterest = Process.GetProcessesByName(app.DisplayName).FirstOrDefault();

                if (processOfInterest is null)
                {
                    logger.LogWarning("Process named {appName} was not found in active processes.", app.DisplayName);
                    await StartApp(app.PathToExe);
                    logger.LogInformation("Started app {appName} from {path}.", app.DisplayName, app.PathToExe);
                    return;
                }

                if (!processOfInterest.Responding)
                {
                    logger.LogWarning("Process named {appName} is not responding.", app.DisplayName);
                    processOfInterest.Kill();
                    await Task.Delay(coreSettings.TimeoutAfterKillInSecs * 1000);
                    await StartApp(app.PathToExe);
                }

                if (processOfInterest.HasExited)
                {
                    logger.LogWarning("Process named {appName} has been terminated.", app.DisplayName);
                }

                logger.LogInformation("Found {appName}/{processName} as running OK.", app.DisplayName, processOfInterest.ProcessName);
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
