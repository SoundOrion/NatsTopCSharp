﻿using System;
using System.Collections.Generic;
using System.Text;
using Spectre.Console;
using NatsTopCSharp.Models;

namespace NatsTopCSharp.Services;

public class DisplayService
{
    public void RenderStats(Engine engine, Stats stats)
    {
        Varz varz = stats.Varz;

        var serverInfo = new StringBuilder();
        serverInfo.AppendLine($"[bold]NATS server version:[/] {varz.Version} (uptime: {varz.Uptime}) {stats.Error}");
        serverInfo.AppendLine($"[bold]Server:[/] {varz.Name}");
        serverInfo.AppendLine($"[bold]ID:[/] {varz.ID}");
        serverInfo.AppendLine($"[bold]Load:[/] CPU: {varz.CPU:F1}%  Memory: {Utilities.Psize(false, varz.Mem)}  Slow Consumers: {varz.SlowConsumers}");
        serverInfo.AppendLine($"[bold]In:[/]  Msgs: {Utilities.Nsize(engine.DisplayRawBytes, varz.InMsgs)}  Bytes: {Utilities.Psize(engine.DisplayRawBytes, varz.InBytes)}  Msgs/Sec: {stats.Rates?.InMsgsRate:F1}  Bytes/Sec: {Utilities.Psize(engine.DisplayRawBytes, (long)(stats.Rates?.InBytesRate ?? 0))}");
        serverInfo.AppendLine($"[bold]Out:[/] Msgs: {Utilities.Nsize(engine.DisplayRawBytes, varz.OutMsgs)}  Bytes: {Utilities.Psize(engine.DisplayRawBytes, varz.OutBytes)}  Msgs/Sec: {stats.Rates?.OutMsgsRate:F1}  Bytes/Sec: {Utilities.Psize(engine.DisplayRawBytes, (long)(stats.Rates?.OutBytesRate ?? 0))}");
        serverInfo.AppendLine($"Connections Polled: {stats.Connz.NumConns}");
        var panel = new Panel(serverInfo.ToString())
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader("Server Info", Justify.Center)
        };
        AnsiConsole.Write(panel);

        var table = new Table
        {
            Border = TableBorder.Rounded,
            Expand = true
        };

        table.AddColumn("HOST");
        table.AddColumn("CID");
        table.AddColumn("NAME");
        table.AddColumn("SUBS");
        table.AddColumn("PENDING");

        if (!engine.ShowRates)
        {
            table.AddColumn("MSGS_TO");
            table.AddColumn("MSGS_FROM");
            table.AddColumn("BYTES_TO");
            table.AddColumn("BYTES_FROM");
        }
        else
        {
            table.AddColumn("OUT_MSGS_RATE");
            table.AddColumn("IN_MSGS_RATE");
            table.AddColumn("OUT_BYTES_RATE");
            table.AddColumn("IN_BYTES_RATE");
        }

        table.AddColumn("LANG");
        table.AddColumn("VERSION");
        table.AddColumn("UPTIME");
        table.AddColumn("LAST_ACTIVITY");
        if (engine.DisplaySubs)
        {
            table.AddColumn("SUBSCRIPTIONS");
        }

        if (stats.Connz?.Conns != null)
        {
            foreach (var conn in stats.Connz.Conns)
            {
                string hostname = engine.LookupDNS ? Utilities.DNSLookup(conn.IP) : $"{conn.IP}:{conn.Port}";
                string cid = conn.Cid.ToString();
                string name = conn.Name ?? "";
                string subsCount = conn.NumSubs.ToString();
                string pending = Utilities.Nsize(engine.DisplayRawBytes, conn.Pending);
                string colMsgsTo, colMsgsFrom, colBytesTo, colBytesFrom;

                if (!engine.ShowRates)
                {
                    colMsgsTo = Utilities.Nsize(engine.DisplayRawBytes, conn.OutMsgs);
                    colMsgsFrom = Utilities.Nsize(engine.DisplayRawBytes, conn.InMsgs);
                    colBytesTo = Utilities.Psize(engine.DisplayRawBytes, conn.OutBytes);
                    colBytesFrom = Utilities.Psize(engine.DisplayRawBytes, conn.InBytes);
                }
                else
                {
                    Rates rates = stats.Rates;
                    ConnRates cr = (rates != null && rates.Connections.ContainsKey(conn.Cid))
                        ? rates.Connections[conn.Cid] : new ConnRates();
                    colMsgsTo = Utilities.Nsize(engine.DisplayRawBytes, (long)cr.OutMsgsRate);
                    colMsgsFrom = Utilities.Nsize(engine.DisplayRawBytes, (long)cr.InMsgsRate);
                    colBytesTo = Utilities.Psize(engine.DisplayRawBytes, (long)cr.OutBytesRate);
                    colBytesFrom = Utilities.Psize(engine.DisplayRawBytes, (long)cr.InBytesRate);
                }

                string lang = conn.Lang;
                string version = conn.Version;
                string uptime = conn.Uptime;
                string lastActivity = Utilities.FormatDateTime(conn.LastActivity);
                string subscriptions = engine.DisplaySubs ? (conn.Subs != null ? string.Join(", ", conn.Subs) : "") : "";

                var row = new List<string>
                {
                    hostname,
                    cid,
                    name,
                    subsCount,
                    pending
                };

                if (!engine.ShowRates)
                {
                    row.Add(colMsgsTo);
                    row.Add(colMsgsFrom);
                    row.Add(colBytesTo);
                    row.Add(colBytesFrom);
                }
                else
                {
                    row.Add(colMsgsTo);
                    row.Add(colMsgsFrom);
                    row.Add(colBytesTo);
                    row.Add(colBytesFrom);
                }

                row.Add(lang);
                row.Add(version);
                row.Add(uptime);
                row.Add(lastActivity);
                if (engine.DisplaySubs)
                {
                    row.Add(subscriptions);
                }
                table.AddRow(row.ToArray());
            }
        }
        AnsiConsole.Write(table);
    }

    public string GenerateParagraphPlainText(Engine engine, Stats stats)
    {
        Varz varz = stats.Varz;
        double inMsgsRate = stats.Rates?.InMsgsRate ?? 0;
        double outMsgsRate = stats.Rates?.OutMsgsRate ?? 0;
        string inBytesRate = Utilities.Psize(engine.DisplayRawBytes, (long)(stats.Rates?.InBytesRate ?? 0));
        string outBytesRate = Utilities.Psize(engine.DisplayRawBytes, (long)(stats.Rates?.OutBytesRate ?? 0));

        var sb = new StringBuilder();
        sb.AppendLine($"NATS server version {varz.Version} (uptime: {varz.Uptime}) {stats.Error}");
        sb.AppendLine($"Server: {varz.Name}");
        sb.AppendLine($"  ID:   {varz.ID}");
        sb.AppendLine($"  Load: CPU:  {varz.CPU:F1}%  Memory: {Utilities.Psize(false, varz.Mem)}  Slow Consumers: {varz.SlowConsumers}");
        sb.AppendLine($"  In:   Msgs: {Utilities.Nsize(engine.DisplayRawBytes, varz.InMsgs)}  Bytes: {Utilities.Psize(engine.DisplayRawBytes, varz.InBytes)}  Msgs/Sec: {inMsgsRate:F1}  Bytes/Sec: {inBytesRate}");
        sb.AppendLine($"  Out:  Msgs: {Utilities.Nsize(engine.DisplayRawBytes, varz.OutMsgs)}  Bytes: {Utilities.Psize(engine.DisplayRawBytes, varz.OutBytes)}  Msgs/Sec: {outMsgsRate:F1}  Bytes/Sec: {outBytesRate}");
        sb.AppendLine();
        sb.AppendLine($"Connections Polled: {stats.Connz.NumConns}");
        sb.AppendLine("HOST\tCID\tNAME\tSUBS\tPENDING\tMSGS_TO\tMSGS_FROM\tBYTES_TO\tBYTES_FROM\tLANG\tVERSION\tUPTIME\tLAST_ACTIVITY");
        if (stats.Connz?.Conns != null)
        {
            foreach (var conn in stats.Connz.Conns)
            {
                string hostname = engine.LookupDNS ? Utilities.DNSLookup(conn.IP) : $"{conn.IP}:{conn.Port}";
                sb.Append($"{hostname}\t{conn.Cid}\t{conn.Name}\t{conn.NumSubs}\t{Utilities.Nsize(engine.DisplayRawBytes, conn.Pending)}\t");
                if (!engine.ShowRates)
                {
                    sb.Append($"{Utilities.Nsize(engine.DisplayRawBytes, conn.OutMsgs)}\t{Utilities.Nsize(engine.DisplayRawBytes, conn.InMsgs)}\t");
                    sb.Append($"{Utilities.Psize(engine.DisplayRawBytes, conn.OutBytes)}\t{Utilities.Psize(engine.DisplayRawBytes, conn.InBytes)}\t");
                }
                else
                {
                    Rates rates = stats.Rates;
                    ConnRates cr = (rates != null && rates.Connections.ContainsKey(conn.Cid))
                        ? rates.Connections[conn.Cid] : new ConnRates();
                    sb.Append($"{Utilities.Nsize(engine.DisplayRawBytes, (long)cr.OutMsgsRate)}\t{Utilities.Nsize(engine.DisplayRawBytes, (long)cr.InMsgsRate)}\t");
                    sb.Append($"{Utilities.Psize(engine.DisplayRawBytes, (long)cr.OutBytesRate)}\t{Utilities.Psize(engine.DisplayRawBytes, (long)cr.InBytesRate)}\t");
                }
                sb.AppendLine($"{conn.Lang}\t{conn.Version}\t{conn.Uptime}\t{Utilities.FormatDateTime(conn.LastActivity)}");
            }
        }
        return sb.ToString();
    }

    public string GenerateParagraphCSV(Engine engine, Stats stats, string delimiter)
    {
        Varz varz = stats.Varz;
        var sb = new StringBuilder();
        sb.AppendLine($"NATS server version{delimiter}{varz.Version}{delimiter}(uptime: {varz.Uptime}){delimiter}{stats.Error}");
        sb.AppendLine("Server:");
        sb.AppendLine($"Load{delimiter}CPU{delimiter}{varz.CPU:F1}%{delimiter}Memory{delimiter}{Utilities.Psize(false, varz.Mem)}{delimiter}Slow Consumers{delimiter}{varz.SlowConsumers}");
        sb.AppendLine($"In{delimiter}Msgs{delimiter}{Utilities.Nsize(engine.DisplayRawBytes, varz.InMsgs)}{delimiter}Bytes{delimiter}{Utilities.Psize(engine.DisplayRawBytes, varz.InBytes)}{delimiter}Msgs/Sec{delimiter}{stats.Rates?.InMsgsRate:F1}{delimiter}Bytes/Sec{delimiter}{Utilities.Psize(engine.DisplayRawBytes, (long)stats.Rates?.InBytesRate)}");
        sb.AppendLine($"Out{delimiter}Msgs{delimiter}{Utilities.Nsize(engine.DisplayRawBytes, varz.OutMsgs)}{delimiter}Bytes{delimiter}{Utilities.Psize(engine.DisplayRawBytes, varz.OutBytes)}{delimiter}Msgs/Sec{delimiter}{stats.Rates?.OutMsgsRate:F1}{delimiter}Bytes/Sec{delimiter}{Utilities.Psize(engine.DisplayRawBytes, (long)stats.Rates?.OutBytesRate)}");
        sb.AppendLine();
        sb.AppendLine($"Connections Polled{delimiter}{stats.Connz.NumConns}");

        List<string> headers = new()
        {
            "HOST", "CID", "NAME", "SUBS", "PENDING",
            "MSGS_TO", "MSGS_FROM", "BYTES_TO", "BYTES_FROM", "LANG", "VERSION", "UPTIME", "LAST_ACTIVITY"
        };
        if (engine.DisplaySubs)
        {
            headers.Add("SUBSCRIPTIONS");
        }
        sb.AppendLine(string.Join(delimiter, headers));

        foreach (var conn in stats.Connz.Conns)
        {
            string hostname = engine.LookupDNS ? Utilities.DNSLookup(conn.IP) : $"{conn.IP}:{conn.Port}";
            List<string> row = new()
            {
                hostname,
                conn.Cid.ToString(),
                conn.Name ?? "",
                conn.NumSubs.ToString(),
                Utilities.Nsize(engine.DisplayRawBytes, conn.Pending)
            };

            if (!engine.ShowRates)
            {
                row.Add(Utilities.Nsize(engine.DisplayRawBytes, conn.OutMsgs));
                row.Add(Utilities.Nsize(engine.DisplayRawBytes, conn.InMsgs));
                row.Add(Utilities.Psize(engine.DisplayRawBytes, conn.OutBytes));
                row.Add(Utilities.Psize(engine.DisplayRawBytes, conn.InBytes));
            }
            else
            {
                Rates rates = stats.Rates;
                ConnRates cr = (rates != null && rates.Connections.ContainsKey(conn.Cid))
                    ? rates.Connections[conn.Cid] : new ConnRates();
                row.Add(Utilities.Nsize(engine.DisplayRawBytes, (long)cr.OutMsgsRate));
                row.Add(Utilities.Nsize(engine.DisplayRawBytes, (long)cr.InMsgsRate));
                row.Add(Utilities.Psize(engine.DisplayRawBytes, (long)cr.OutBytesRate));
                row.Add(Utilities.Psize(engine.DisplayRawBytes, (long)cr.InBytesRate));
            }
            row.Add(conn.Lang);
            row.Add(conn.Version);
            row.Add(conn.Uptime);
            row.Add(conn.LastActivity);
            if (engine.DisplaySubs)
            {
                string subs = conn.Subs != null ? string.Join(", ", conn.Subs) : "";
                row.Add(subs);
            }
            sb.AppendLine(string.Join(delimiter, row));
        }
        return sb.ToString();
    }
}
