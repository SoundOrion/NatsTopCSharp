using System;
using System.Collections.Generic;
using System.Net;

namespace NatsTopCSharp;

/// <summary>
/// ヘルパーメソッド群（DNS ルックアップ、サイズフォーマット、日時フォーマットなど）をまとめた静的クラス
/// </summary>
public static class Utilities
{
    private static readonly Dictionary<string, string> dnsCache = new();

    /// <summary>
    /// DNS ルックアップ（結果はキャッシュする）
    /// </summary>
    public static string DNSLookup(string ip)
    {
        if (dnsCache.TryGetValue(ip, out string value))
            return value;
        try
        {
            var entry = Dns.GetHostEntry(ip);
            string hostname = entry.HostName;
            dnsCache[ip] = hostname;
            return hostname;
        }
        catch
        {
            dnsCache[ip] = ip;
            return ip;
        }
    }

    /// <summary>
    /// バイトサイズを人間に読みやすい文字列に変換
    /// </summary>
    public static string Psize(bool displayRawValue, long s)
    {
        double size = s;
        const double kibibyte = 1024;
        const double mebibyte = 1024 * 1024;
        const double gibibyte = 1024 * 1024 * 1024;

        if (displayRawValue || size < kibibyte)
            return $"{size:0}";
        if (size < mebibyte)
            return $"{size / kibibyte:0.0}K";
        if (size < gibibyte)
            return $"{size / mebibyte:0.0}M";
        return $"{size / gibibyte:0.0}G";
    }

    /// <summary>
    /// 数値を読みやすい形式に変換（K, M, B, T 単位）
    /// </summary>
    public static string Nsize(bool displayRawValue, long s)
    {
        double size = s;
        const double k = 1000;
        const double m = k * 1000;
        const double b = m * 1000;
        const double t = b * 1000;

        if (displayRawValue || size < k)
            return $"{size:0}";
        if (size < m)
            return $"{size / k:0.0}K";
        if (size < b)
            return $"{size / m:0.0}M";
        if (size < t)
            return $"{size / b:0.0}B";
        return $"{size / t:0.0}T";
    }

    /// <summary>
    /// 日付フォーマットを変換
    /// </summary>
    public static string FormatDateTime(this string isoDateTime)
    {
        try
        {
            var dto = DateTimeOffset.Parse(isoDateTime);
            return dto.DateTime.ToString("yyyy/MM/dd HH:mm");
        }
        catch
        {
            return isoDateTime;
        }
    }
}
