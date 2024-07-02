namespace AppKeeperService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            builder.ConfigureServices();

            builder.Services.AddHostedService<WindowsBackgroundService>();

            var host = builder.Build();
            host.Run();
        }
    }
}