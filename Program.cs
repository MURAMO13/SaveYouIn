namespace SaveYouIn;
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var host = new HostBuilder()
            .ConfigureServices((hostContext, services) => ConfigureServices(services))
            .UseConsoleLifetime()
            .Build();

        Console.WriteLine("Сервис запущен");

        await host.RunAsync();
        Console.WriteLine("Сервис остановлен");
    }

    static void ConfigureServices(IServiceCollection services)
    {
        AppConfigurations appConfigurations = new AppConfigurations() { BotToken = "############" };
        services.AddSingleton(appConfigurations);
        services.AddSingleton<ITelegramBotClient, TelegramBotClient>(provider => new TelegramBotClient(appConfigurations.BotToken));

        services.AddHostedService<SaveYouInBot>();
        services.AddTransient<YouTubeLinkController>();
        services.AddTransient<InstaLinkController>();
    }
}
