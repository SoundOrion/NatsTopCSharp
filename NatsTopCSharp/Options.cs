using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace NatsTopCSharp;

/// <summary>
/// コマンドラインオプション
/// コマンドライン引数のパース用クラス
/// </summary>
public class Options
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8222;
    public int HttpsPort { get; set; } = 0;
    public int Conns { get; set; } = 1024;
    public int Delay { get; set; } = 1;
    public string SortBy { get; set; } = "cid";
    public bool LookupDNS { get; set; } = false;
    public string OutputFile { get; set; } = "";
    public string OutputDelimiter { get; set; } = "";
    public bool DisplayRawBytes { get; set; } = false;
    public int MaxStatsRefreshes { get; set; } = -1;
    public bool ShowVersion { get; set; } = false;
    public bool DisplaySubscriptionsColumn { get; set; } = false;
    public string Cert { get; set; } = "";
    public string Key { get; set; } = "";
    public string CACert { get; set; } = "";
    public bool SkipVerify { get; set; } = false;
    public string Version { get; set; } = "0.0.0";

    public static async Task<Options> ParseAsync(string[] args)
    {
        var rootCommand = new RootCommand("NATS Top CSharp CLI");

        // 基本オプション
        var hostOption = new Option<string>("-s", "NATS サーバーのホストアドレスを指定します。") { ArgumentHelpName = "ホスト", IsRequired = false };
        hostOption.SetDefaultValue("127.0.0.1");

        var portOption = new Option<int>("-m", "NATS サーバーのモニタリング用ポート番号を指定します。") { ArgumentHelpName = "ポート", IsRequired = false };
        portOption.SetDefaultValue(8222);

        var connsOption = new Option<int>("-n", "監視対象の最大接続数を指定します。") { ArgumentHelpName = "接続数", IsRequired = false };
        connsOption.SetDefaultValue(1024);

        var delayOption = new Option<int>("-d", "NATS の統計情報を更新する間隔（秒単位）を指定します。") { ArgumentHelpName = "更新間隔", IsRequired = false };
        delayOption.SetDefaultValue(1);

        var sortOption = new Option<string>("-sort", "コネクション情報のソート基準を指定します。") { ArgumentHelpName = "ソート基準", IsRequired = false };
        sortOption.SetDefaultValue("cid");

        var lookupOption = new Option<bool>("-lookup", "クライアントアドレスの DNS ルックアップを有効にします。") { ArgumentHelpName = "DNSルックアップ", IsRequired = false };
        lookupOption.SetDefaultValue(false);

        var outputFileOption = new Option<string>("-o", "初回の NATS 統計情報のスナップショットを指定したファイルに保存し、プログラムを終了します。\n特記事項: '-' を指定すると、スナップショットが標準出力に表示されます。") { ArgumentHelpName = "出力ファイル", IsRequired = false };
        outputFileOption.SetDefaultValue("");

        var outputDelimiterOption = new Option<string>("-l", "-o オプションで指定された出力ファイルのデリミタ（区切り文字）を設定します。\nデフォルト値: ''（未指定の場合、標準のグリッド形式で出力）") { ArgumentHelpName = "デリミタ", IsRequired = false };
        outputDelimiterOption.SetDefaultValue("");

        var rawBytesOption = new Option<bool>("-b", "ネットワークトラフィックを生のバイト数で表示します。") { ArgumentHelpName = "生バイト", IsRequired = false };
        rawBytesOption.SetDefaultValue(false);

        var maxStatsOption = new Option<int>("-r", "NATS の統計情報を更新する最大回数を指定します。") { ArgumentHelpName = "最大更新回数", IsRequired = false };
        maxStatsOption.SetDefaultValue(-1);

        var versionOption = new Option<bool>(["-v"], "プログラムのバージョン情報を表示します。") { ArgumentHelpName = "バージョン", IsRequired = false };
        versionOption.SetDefaultValue(false);

        var displaySubscriptionsOption = new Option<bool>(["-u", "--display-subscriptions-column"], "購読数のカラムを表示します。") { ArgumentHelpName = "購読カラム", IsRequired = false };
        displaySubscriptionsOption.SetDefaultValue(false);

        // セキュアオプション（TLS関連）
        var httpsPortOption = new Option<int>("-ms", "NATS サーバーの HTTPS モニタリング用ポートを指定します。") { ArgumentHelpName = "HTTPSポート", IsRequired = false };
        httpsPortOption.SetDefaultValue(0);

        var certOption = new Option<string>("-cert", "NATS サーバーが TLS を使用している場合のクライアント証明書のパスを指定します。") { ArgumentHelpName = "証明書", IsRequired = false };
        certOption.SetDefaultValue("");

        var keyOption = new Option<string>("-key", "NATS サーバーが TLS を使用している場合のクライアント秘密鍵のパスを指定します。") { ArgumentHelpName = "キー", IsRequired = false };
        keyOption.SetDefaultValue("");

        var caCertOption = new Option<string>("-cacert", "ルート CA 証明書のパスを指定します。") { ArgumentHelpName = "CA 証明書", IsRequired = false };
        caCertOption.SetDefaultValue("");

        var skipVerifyOption = new Option<bool>("-k", "サーバー証明書の検証をスキップします（自己署名証明書を使用する場合など）。") { ArgumentHelpName = "検証スキップ", IsRequired = false };
        skipVerifyOption.SetDefaultValue(false);

        var helpOption = new Option<bool>(["-h", "--help"], "このヘルプを表示します。");

        // ルートコマンドにオプションを追加
        rootCommand.AddOption(hostOption);
        rootCommand.AddOption(portOption);
        rootCommand.AddOption(httpsPortOption);
        rootCommand.AddOption(connsOption);
        rootCommand.AddOption(delayOption);
        rootCommand.AddOption(sortOption);
        rootCommand.AddOption(lookupOption);
        rootCommand.AddOption(outputFileOption);
        rootCommand.AddOption(outputDelimiterOption);
        rootCommand.AddOption(rawBytesOption);
        rootCommand.AddOption(maxStatsOption);
        rootCommand.AddOption(versionOption);
        rootCommand.AddOption(displaySubscriptionsOption);
        rootCommand.AddOption(certOption);
        rootCommand.AddOption(keyOption);
        rootCommand.AddOption(caCertOption);
        rootCommand.AddOption(skipVerifyOption);
        rootCommand.AddOption(helpOption);

        // コマンドハンドラーの設定
        var options = new Options();
        rootCommand.SetHandler((InvocationContext context) =>
        {
            options.Host = context.ParseResult.GetValueForOption(hostOption);
            options.Port = context.ParseResult.GetValueForOption(portOption);
            options.HttpsPort = context.ParseResult.GetValueForOption(httpsPortOption);
            options.Conns = context.ParseResult.GetValueForOption(connsOption);
            options.Delay = context.ParseResult.GetValueForOption(delayOption);
            options.SortBy = context.ParseResult.GetValueForOption(sortOption);
            options.LookupDNS = context.ParseResult.GetValueForOption(lookupOption);
            options.OutputFile = context.ParseResult.GetValueForOption(outputFileOption);
            options.OutputDelimiter = context.ParseResult.GetValueForOption(outputDelimiterOption);
            options.DisplayRawBytes = context.ParseResult.GetValueForOption(rawBytesOption);
            options.MaxStatsRefreshes = context.ParseResult.GetValueForOption(maxStatsOption);
            options.ShowVersion = context.ParseResult.GetValueForOption(versionOption);
            options.DisplaySubscriptionsColumn = context.ParseResult.GetValueForOption(displaySubscriptionsOption);
            options.Cert = context.ParseResult.GetValueForOption(certOption);
            options.Key = context.ParseResult.GetValueForOption(keyOption);
            options.CACert = context.ParseResult.GetValueForOption(caCertOption);
            options.SkipVerify = context.ParseResult.GetValueForOption(skipVerifyOption);
        });

        await rootCommand.InvokeAsync(args);
        return options;
    }

    //public static Options Parse(string[] args)
    //{
    //    Options opts = new();
    //    for (int i = 0; i < args.Length; i++)
    //    {
    //        string arg = args[i];
    //        if (arg == "-s" && i + 1 < args.Length)
    //        {
    //            opts.Host = args[++i];
    //        }
    //        else if (arg == "-m" && i + 1 < args.Length && int.TryParse(args[++i], out int port))
    //        {
    //            opts.Port = port;
    //        }
    //        else if (arg == "-ms" && i + 1 < args.Length && int.TryParse(args[++i], out int httpsPort))
    //        {
    //            opts.HttpsPort = httpsPort;
    //        }
    //        else if (arg == "-n" && i + 1 < args.Length && int.TryParse(args[++i], out int conns))
    //        {
    //            opts.Conns = conns;
    //        }
    //        else if (arg == "-d" && i + 1 < args.Length && int.TryParse(args[++i], out int delay))
    //        {
    //            opts.Delay = delay;
    //        }
    //        else if (arg == "-sort" && i + 1 < args.Length)
    //        {
    //            opts.SortBy = args[++i];
    //        }
    //        else if (arg == "-lookup")
    //        {
    //            opts.LookupDNS = true;
    //        }
    //        else if (arg == "-o" && i + 1 < args.Length)
    //        {
    //            opts.OutputFile = args[++i];
    //        }
    //        else if (arg == "-l" && i + 1 < args.Length)
    //        {
    //            opts.OutputDelimiter = args[++i];
    //        }
    //        else if (arg == "-b")
    //        {
    //            opts.DisplayRawBytes = true;
    //        }
    //        else if (arg == "-r" && i + 1 < args.Length && int.TryParse(args[++i], out int max))
    //        {
    //            opts.MaxStatsRefreshes = max;
    //        }
    //        else if (arg == "-v" || arg == "--version")
    //        {
    //            opts.ShowVersion = true;
    //        }
    //        else if (arg == "-u" || arg == "--display-subscriptions-column")
    //        {
    //            opts.DisplaySubscriptionsColumn = true;
    //        }
    //        else if (arg == "-cert" && i + 1 < args.Length)
    //        {
    //            opts.Cert = args[++i];
    //        }
    //        else if (arg == "-key" && i + 1 < args.Length)
    //        {
    //            opts.Key = args[++i];
    //        }
    //        else if (arg == "-cacert" && i + 1 < args.Length)
    //        {
    //            opts.CACert = args[++i];
    //        }
    //        else if (arg == "-k")
    //        {
    //            opts.SkipVerify = true;
    //        }
    //    }
    //    return opts;
    //}
}
