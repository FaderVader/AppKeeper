using Serilog;
using Serilog.Events;

namespace AppKeeperService.Models;
public class LoggingSettings
{
    public LogEventLevel LogLevel { get; set; }
    public int RetainedFileCountLimit { get; set; }
    public string OutputTemplate { get; set; }
    public RollingInterval RollingInterval { get; set; }
    public string RelativePath { get; set; }
}
