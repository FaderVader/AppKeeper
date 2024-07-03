
namespace AppKeeperService.Workers;

public interface IAppMonitor
{
    /// <summary>
    /// Query the status of the target-application. <br/>
    /// If not detected, we will attempt to start the process.
    /// </summary>
    /// <returns></returns>
    Task InspectStatus();

    /// <summary>
    /// Start the process in the context of the currently logged-in user.
    /// </summary>
    /// <param name="processName"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    Task<bool> StartApp(string processName, string args = "");
}