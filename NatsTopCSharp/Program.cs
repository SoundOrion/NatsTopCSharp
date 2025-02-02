using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NatsTopCSharp.Services;
using System.Security.Cryptography.X509Certificates;

namespace NatsTopCSharp;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders(); // 既存のログプロバイダを削除
                logging.AddConsole(); // コンソールにログを出力（他のログは有効）
                logging.SetMinimumLevel(LogLevel.Warning); // 警告以上のみ表示
            })
            .ConfigureServices((context, services) =>
            {
                // Options を IOptions<T> で登録
                var options = Options.Parse(args);
                services.AddSingleton(options);
                services.Configure<Options>(opt =>
                {
                    opt.Host = options.Host;
                    opt.Port = options.Port;
                    opt.Conns = options.Conns;
                    opt.Delay = options.Delay;
                });

                // HttpClient を DI に登録
                services.AddHttpClient("nats", client =>
            {
                //client.Timeout = TimeSpan.FromSeconds(10);
                //client.DefaultRequestHeaders.Add("User-Agent", "NatsTopCSharp");
            }).ConfigurePrimaryHttpMessageHandler(() =>
            {
                if (options.HttpsPort != 0)
                {
                    var handler = new HttpClientHandler();

                    // HTTPS の証明書検証をスキップ（開発用）
                    if (options.SkipVerify)
                    {
                        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }

                    // CA 証明書を追加
                    if (!string.IsNullOrEmpty(options.CACert))
                    {
                        var caCert = new X509Certificate2(File.ReadAllBytes(options.CACert));
                        handler.ClientCertificates.Add(caCert);
                    }

                    // クライアント証明書を追加
                    if (!string.IsNullOrEmpty(options.Cert) && !string.IsNullOrEmpty(options.Key))
                    {
                        var cert = new X509Certificate2(options.Cert);
                        handler.ClientCertificates.Add(cert);
                    }

                    return handler;
                }
                return new HttpClientHandler(); // 通常の HTTP 用
            });

                // その他のサービス登録
                services.AddSingleton<Engine>();
                services.AddSingleton<DisplayService>();
                services.AddSingleton<App>();
            })
            .Build();

        // DIで解決したAppクラスを実行
        var app = host.Services.GetRequiredService<App>();
        await app.RunAsync(true);
    }
}