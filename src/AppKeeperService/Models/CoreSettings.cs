namespace AppKeeperService.Models;
public class CoreSettings
{
    public List<MonitoredApplication> ApplicationList { get; set; } = new();
    public int RecheckStatusIntervalInSecs { get; set; }
    public int TimeoutAfterKillInSecs { get; set; }
}
