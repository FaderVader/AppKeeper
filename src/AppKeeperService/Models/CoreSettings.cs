using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppKeeperService.Models;
public class CoreSettings
{
    public List<MonitoredApplication> ApplicationList { get; set; } = new();
    public int RecheckStatusIntervalInSecs { get; set; }
    public int TimeoutAfterKillInSecs { get; set; }
}
