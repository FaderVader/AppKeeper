using AppKeeperService.Service;

namespace AppKeeperService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            builder.ConfigureServices();

            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "AppKeeper";
            });

            var host = builder.Build();
            host.Run();
        }
    }
}