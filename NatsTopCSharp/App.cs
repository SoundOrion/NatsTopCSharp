using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NatsTopCSharp.Services;
using Spectre.Console;

namespace NatsTopCSharp;

/// <summary>
/// App クラスはアプリケーションのメインループを実装し、DIで注入された Options、Engine、DisplayService を利用して処理を実行する
/// </summary>
public class App
{
    private readonly Options _options;
    private readonly Engine _engine;
    private readonly DisplayService _displayService;

    public App(Options options, Engine engine, DisplayService displayService)
    {
        _options = options;
        _engine = engine;
        _displayService = displayService;
    }

    public async Task RunAsync(string displayMode)
    {
        // バージョン表示
        if (_options.ShowVersion)
        {
            Console.WriteLine($"nats-top v{_options.Version}");
            return;
        }

        // Engineのセットアップ
        if (_options.HttpsPort != 0)
        {
            _engine.Port = _options.HttpsPort;
            _engine.SetupHTTPS(_options.CACert, _options.Cert, _options.Key, _options.SkipVerify);
        }
        else
        {
            _engine.SetupHTTP();
        }

        // /varzの問い合わせで初期接続チェック
        try
        {
            var varz = await _engine.RequestVarz();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"nats-top: /varz smoke test failed: {ex.Message}");
            return;
        }

        _engine.SortOpt = _options.SortBy;
        _engine.DisplaySubs = _options.DisplaySubscriptionsColumn;
        _engine.DisplayRawBytes = _options.DisplayRawBytes;
        _engine.LookupDNS = _options.LookupDNS;

        // ファイル出力モードの場合
        if (!string.IsNullOrEmpty(_options.OutputFile))
        {
            var stats = await _engine.FetchStats();
            string text = !string.IsNullOrEmpty(_options.OutputDelimiter)
                ? _displayService.GenerateParagraphCSV(_engine, stats, _options.OutputDelimiter)
                : _displayService.GenerateParagraphPlainText(_engine, stats);
            if (_options.OutputFile == "-")
            {
                Console.WriteLine(text);
            }
            else
            {
                File.WriteAllText(_options.OutputFile, text);
            }
            return;
        }

        using CancellationTokenSource cts = new();
        var monitoringTask = _engine.MonitorStats(cts.Token);

        // カーソルを非表示にする
        Console.CursorVisible = false;

        int refreshCount = 0;
        while (!cts.IsCancellationRequested)
        {
            if (_engine.LastStats != null)
            {
                Console.SetCursorPosition(0, 0); // カーソルを左上に移動

                if (displayMode == "spectre")
                {
                    AnsiConsole.Clear();
                    _displayService.RenderStats(_engine, _engine.LastStats);
                }
                else
                {
                    Console.Clear();
                    string text = _displayService.GenerateParagraphPlainText(_engine, _engine.LastStats);
                    Console.WriteLine(text);
                }

                refreshCount++;
                if (_options.MaxStatsRefreshes > 0 && refreshCount >= _options.MaxStatsRefreshes)
                {
                    break;
                }
            }

            // キー入力のチェック（qまたはCtrl+Cで終了、spaceでレート表示切替、sでサブスクリプション列切替、dでDNSルックアップ切替、bでバイト表記切替）
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q || (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control)))
                {
                    break;
                }
                else if (key.Key == ConsoleKey.Spacebar)
                {
                    _engine.ShowRates = !_engine.ShowRates;
                }
                else if (key.Key == ConsoleKey.S)
                {
                    _engine.DisplaySubs = !_engine.DisplaySubs;
                }
                else if (key.Key == ConsoleKey.D)
                {
                    _engine.LookupDNS = !_engine.LookupDNS;
                }
                else if (key.Key == ConsoleKey.B)
                {
                    _engine.DisplayRawBytes = !_engine.DisplayRawBytes;
                }
            }

            await Task.Delay(500);
        }

        cts.Cancel();
        await monitoringTask;
    }
}
