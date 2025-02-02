using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NatsTopCSharp.Models;

namespace NatsTopCSharp.Services;

/// <summary>
/// NATS サーバーから統計情報を取得・計算するエンジンクラス
/// </summary>
public class Engine
{
    public string Host { get; set; }
    public int Port { get; set; }
    public int Conns { get; set; }
    public int Delay { get; set; }
    public string Uri { get; set; }
    public string SortOpt { get; set; }
    public bool DisplaySubs { get; set; } = false;
    public bool ShowRates { get; set; } = false;
    public bool DisplayRawBytes { get; set; } = false;
    public bool LookupDNS { get; set; } = false;

    public Models.Stats LastStats { get; set; }
    public Dictionary<ulong, Models.ConnInfo> LastConnz { get; set; } = new();

    public HttpClient HttpClient { get; set; }

    private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    //private readonly IHttpClientFactory _httpClientFactory;
    //private readonly Options _options;

    public Engine(IHttpClientFactory httpClientFactory, IOptions<Options> options)
    {
        //_httpClientFactory = httpClientFactory;
        HttpClient = httpClientFactory.CreateClient();

        var option = options.Value;
        Host = option.Host;
        Port = option.Port;
        Conns = option.Conns;
        Delay = option.Delay;
    }

    public void SetupHTTP()
    {
        Uri = $"http://{Host}:{Port}";
    }

    public void SetupHTTPS()
    {
        Uri = $"https://{Host}:{Port}";
    }

    public async Task<object> Request(string path)
    {
        string url = Uri + path;
        if (path.StartsWith("/connz"))
        {
            url += $"?limit={Conns}&sort={SortOpt}";
            if (DisplaySubs)
            {
                url += "&subs=1";
            }
        }
        HttpResponseMessage response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        if (path == "/varz")
        {
            Varz varz = JsonSerializer.Deserialize<Varz>(body, jsonOptions);
            return varz;
        }
        else if (path.StartsWith("/connz"))
        {
            Connz connz = JsonSerializer.Deserialize<Connz>(body, jsonOptions);
            return connz;
        }
        else
        {
            throw new Exception($"invalid path '{path}'");
        }
    }
    public async Task<Varz> RequestVarz() => (Varz)await Request("/varz");

    public async Task<Stats> FetchStats()
    {
        Stats stats = new Stats();
        try
        {
            Task<object> varzTask = Request("/varz");
            Task<object> connzTask = Request("/connz");
            await Task.WhenAll(varzTask, connzTask);
            stats.Varz = (Varz)varzTask.Result;
            stats.Connz = (Connz)connzTask.Result;
        }
        catch (Exception ex)
        {
            stats.Error = ex.Message;
            return stats;
        }

        if (LastStats != null)
        {
            TimeSpan tdelta = stats.Varz.Now - LastStats.Varz.Now;
            if (tdelta.TotalSeconds > 0)
            {
                long inMsgsDelta = stats.Varz.InMsgs - LastStats.Varz.InMsgs;
                long outMsgsDelta = stats.Varz.OutMsgs - LastStats.Varz.OutMsgs;
                long inBytesDelta = stats.Varz.InBytes - LastStats.Varz.InBytes;
                long outBytesDelta = stats.Varz.OutBytes - LastStats.Varz.OutBytes;

                Rates rates = new Rates
                {
                    InMsgsRate = inMsgsDelta / tdelta.TotalSeconds,
                    OutMsgsRate = outMsgsDelta / tdelta.TotalSeconds,
                    InBytesRate = inBytesDelta / tdelta.TotalSeconds,
                    OutBytesRate = outBytesDelta / tdelta.TotalSeconds,
                    Connections = []
                };

                if (stats.Connz?.Conns != null)
                {
                    foreach (var conn in stats.Connz.Conns)
                    {
                        Models.ConnRates cr = new Models.ConnRates();
                        if (LastConnz.ContainsKey(conn.Cid))
                        {
                            Models.ConnInfo lastConn = LastConnz[conn.Cid];
                            cr.InMsgsRate = (conn.InMsgs - lastConn.InMsgs);
                            cr.OutMsgsRate = (conn.OutMsgs - lastConn.OutMsgs);
                            cr.InBytesRate = (conn.InBytes - lastConn.InBytes);
                            cr.OutBytesRate = (conn.OutBytes - lastConn.OutBytes);
                        }
                        rates.Connections[conn.Cid] = cr;
                    }
                }
                stats.Rates = rates;
            }
        }

        LastStats = stats;
        LastConnz.Clear();
        if (stats.Connz?.Conns != null)
        {
            foreach (var conn in stats.Connz.Conns)
            {
                LastConnz[conn.Cid] = conn;
            }
        }
        return stats;
    }

    public async Task MonitorStats(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await FetchStats();
            await Task.Delay(Delay * 1000, token);
        }
    }
}
