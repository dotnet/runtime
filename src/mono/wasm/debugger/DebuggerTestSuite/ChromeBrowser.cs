// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Microsoft.WebAssembly.Diagnostics;

#nullable enable

namespace DebuggerTests;

internal class ChromeBrowser : BrowserBase
{
    static readonly Regex s_parseConnection = new (@"listening on (ws?s://[^\s]*)");

    public ChromeBrowser(ILogger logger) : base(logger)
    {
    }

    public override async Task Launch(HttpContext context,
                                      string browserPath,
                                      string url,
                                      int remoteDebuggingPort,
                                      string test_id,
                                      string message_prefix,
                                      int browser_ready_timeout_ms = 20000)
    {
        ProcessStartInfo psi = GetProcessStartInfo(browserPath, GetInitParms(remoteDebuggingPort), url);
        (Process proc, string line) = await LaunchBrowser(
                                psi,
                                context,
                                str =>
                                {
                                    Match match = s_parseConnection.Match(str);
                                    return match.Success
                                                ? match.Groups[1].Captures[0].Value
                                                : null;
                                },
                                message_prefix,
                                browser_ready_timeout_ms);

        if (proc is null || line is null)
            throw new Exception($"Failed to launch chrome");

        string con_str = await ExtractConnUrl(line, logger);

        logger.LogInformation($"{message_prefix} launching proxy for {con_str}");
        string logFilePath = Path.Combine(DebuggerTestBase.TestLogPath, $"{test_id}-proxy.log");
        File.Delete(logFilePath);

        var proxyLoggerFactory = LoggerFactory.Create(
            builder => builder
                    // .AddSimpleConsole(options =>
                    //     {
                    //         options.SingleLine = true;
                    //         options.TimestampFormat = "[HH:mm:ss] ";
                    //     })
                .AddFile(logFilePath, minimumLevel: LogLevel.Trace)
                .AddFilter(null, LogLevel.Trace));

        var proxy = new DebuggerProxy(proxyLoggerFactory, null, loggerId: test_id);
        var browserUri = new Uri(con_str);
        WebSocket? ideSocket = null;
        try
        {
            ideSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            await proxy.Run(browserUri, ideSocket).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError($"{message_prefix} {ex}");
            logger.LogDebug($"aborting the socket");
            ideSocket?.Abort();
            ideSocket?.Dispose();
            throw;
        }
        finally
        {
            logger.LogDebug($"Killing process");
            proc.CancelErrorRead();
            proc.CancelOutputRead();
            proc.Kill();
            proc.WaitForExit();
            proc.Close();
        }
    }

    internal virtual async Task<string> ExtractConnUrl (string str, ILogger logger)
    {
        var client = new HttpClient();
        var start = DateTime.Now;
        JArray? obj = null;

        while (true)
        {
            // Unfortunately it does look like we have to wait
            // for a bit after getting the response but before
            // making the list request.  We get an empty result
            // if we make the request too soon.
            await Task.Delay(100);

            var res = await client.GetStringAsync(new Uri(new Uri(str), "/json/list"));
            logger.LogInformation("res is {0}", res);

            if (!string.IsNullOrEmpty(res))
            {
                // Sometimes we seem to get an empty array `[ ]`
                obj = JArray.Parse(res);
                if (obj != null && obj.Count >= 1)
                    break;
            }

            var elapsed = DateTime.Now - start;
            if (elapsed.Milliseconds > 5000)
            {
                string message = $"Unable to get DevTools /json/list response in {elapsed.Seconds} seconds, stopping";
                logger.LogError(message);
                throw new Exception(message);
            }
        }

        string? wsURl = obj[0]?["webSocketDebuggerUrl"]?.Value<string>();
        if (wsURl is null)
            throw new Exception($"Could not get the webSocketDebuggerUrl in {obj}");

        logger.LogTrace(">>> {0}", wsURl);

        return wsURl;
    }

    private static string GetInitParms(int port)
    {
        string str = $"--headless --disable-gpu --lang=en-US --incognito --remote-debugging-port={port}";
        if (File.Exists("/.dockerenv"))
        {
            Console.WriteLine ("Detected a container, disabling sandboxing for debugger tests.");
            str = "--no-sandbox " + str;
        }
        return str;
    }
}
