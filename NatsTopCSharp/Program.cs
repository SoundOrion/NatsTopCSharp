using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NatsTopCSharp.Services;

namespace NatsTopCSharp;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Optionsはコマンドライン引数からパースしてシングルトンとして登録
                var options = Options.Parse(args);
                services.AddSingleton(options);

                // Engine、DisplayService、App を登録
                services.AddSingleton(new Engine(options.Host, options.Port, options.Conns, options.Delay));
                services.AddSingleton<DisplayService>();
                services.AddSingleton<App>();
            })
            .Build();

        // DIで解決したAppクラスを実行
        var app = host.Services.GetRequiredService<App>();
        await app.RunAsync(displayMode: "");
    }
}