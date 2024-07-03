using AppKeeperService.Models;
using AppKeeperService.Workers;
using Serilog;

namespace AppKeeperService;
public static class RegisterServices
{
    public static void ConfigureServices(this HostApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appSettings.json", optional: false, reloadOnChange: false);

        var logSettings = new LoggingSettings();
        builder.Configuration.GetSection("LoggingSettings").Bind(logSettings);
        var logPath = Path.Combine(builder.Environment.ContentRootPath, logSettings.RelativePath, $"{builder.Environment.ApplicationName}");

        builder.Services.AddSerilog(lc => lc
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                logPath,
                retainedFileCountLimit: logSettings.RetainedFileCountLimit,
                rollingInterval: logSettings.RollingInterval,
                restrictedToMinimumLevel: logSettings.LogLevel,
                outputTemplate: logSettings.OutputTemplate)
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            );
        
        // Setup configuration for DI
        builder.Services.Configure<CoreSettings>(builder.Configuration.GetSection(nameof(CoreSettings)));
        builder.Services.AddSingleton<IAppMonitor, AppMonitor>();
    }
}
