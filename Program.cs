using Telegram.Bot;

namespace UtilityBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ISessionStorage, MemorySessionStorage>();
                    services.AddSingleton<ITelegramBotClient>((_) =>
                        new TelegramBotClient(token: context.Configuration.GetValue("BotAPIToken", string.Empty)!));
                    services.AddTransient<MenuController>();
                    services.AddHostedService<BotWorker>();
                })
                .Build();
            host.Run();
        }
    }
}