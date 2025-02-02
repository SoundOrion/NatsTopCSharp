using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NatsTopCSharp.Services;

namespace NatsTopCSharp;

class Program
{
    static async Task Main(string[] args)
    {
        // DIコンテナを構築
        var serviceCollection = new ServiceCollection();

        // Optionsはコマンドライン引数からパースしてシングルトンとして登録
        var options = Options.Parse(args);
        serviceCollection.AddSingleton(options);


        // Engine、DisplayService、App を登録
        serviceCollection.AddSingleton(new Engine(options.Host, options.Port, options.Conns, options.Delay));
        serviceCollection.AddSingleton<DisplayService>();
        serviceCollection.AddSingleton<App>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        // DIで解決したAppクラスを実行
        var app = serviceProvider.GetRequiredService<App>();
        await app.RunAsync(displayMode:"");
    }
}
