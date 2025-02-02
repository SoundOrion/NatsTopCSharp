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

namespace NatsTopCSharp.Services;

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

    // DI用のパラメータレスコンストラクタ
    public Engine() { }

    public Engine(string host, int port, int conns, int delay)
    {
        Host = host;
        Port = port;
        Conns = conns;
        Delay = delay;
    }

    public void SetupHTTP()
    {
        HttpClient = new HttpClient();
        Uri = $"http://{Host}:{Port}";
    }

    public void SetupHTTPS(string caCertOpt, string certOpt, string keyOpt, bool skipVerify)
    {
        HttpClientHandler handler = new HttpClientHandler();
        if (skipVerify)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        if (!string.IsNullOrEmpty(caCertOpt))
        {
            var caCert = new X509Certificate2(File.ReadAllBytes(caCertOpt));
            handler.ClientCertificates.Add(caCert);
        }
        if (!string.IsNullOrEmpty(certOpt) && !string.IsNullOrEmpty(keyOpt))
        {
            var cert = new X509Certificate2(certOpt);
            handler.ClientCertificates.Add(cert);
        }
        HttpClient = new HttpClient(handler);
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
            Models.Varz varz = JsonSerializer.Deserialize<Models.Varz>(body, jsonOptions);
            return varz;
        }
        else if (path.StartsWith("/connz"))
        {
            Models.Connz connz = JsonSerializer.Deserialize<Models.Connz>(body, jsonOptions);
            return connz;
        }
        else
        {
            throw new Exception($"invalid path '{path}'");
        }
    }
    public async Task<Models.Varz> RequestVarz() => (Models.Varz)await Request("/varz");

    public async Task<Models.Stats> FetchStats()
    {
        Models.Stats stats = new Models.Stats();
        try
        {
            Task<object> varzTask = Request("/varz");
            Task<object> connzTask = Request("/connz");
            await Task.WhenAll(varzTask, connzTask);
            stats.Varz = (Models.Varz)varzTask.Result;
            stats.Connz = (Models.Connz)connzTask.Result;
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

                Models.Rates rates = new Models.Rates
                {
                    InMsgsRate = inMsgsDelta / tdelta.TotalSeconds,
                    OutMsgsRate = outMsgsDelta / tdelta.TotalSeconds,
                    InBytesRate = inBytesDelta / tdelta.TotalSeconds,
                    OutBytesRate = outBytesDelta / tdelta.TotalSeconds,
                    Connections = new Dictionary<ulong, Models.ConnRates>()
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
