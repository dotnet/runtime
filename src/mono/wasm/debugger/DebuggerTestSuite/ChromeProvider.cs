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

internal class ChromeProvider : WasmHostProvider
{
    static readonly Regex s_parseConnection = new (@"listening on (ws?s://[^\s]*)");
    private Process? _process;
    private WebSocket? _ideWebSocket;
    private bool _isDisposed;

    public ChromeProvider(string id, ILogger logger) : base(id, logger)
    {
    }

    public async Task StartBrowserAndProxy(HttpContext context,
                                      string browserPath,
                                      string targetUrl,
                                      int remoteDebuggingPort,
                                      string messagePrefix,
                                      ILoggerFactory loggerFactory,
                                      int browserReadyTimeoutMs = 20000)
    {
        ProcessStartInfo psi = GetProcessStartInfo(browserPath, GetInitParms(remoteDebuggingPort), targetUrl);
        (Process? proc, string? line) = await LaunchHost(
                                psi,
                                context,
                                str =>
                                {
                                    if (string.IsNullOrEmpty(str))
                                        return null;

                                    Match match = s_parseConnection.Match(str);
                                    return match.Success
                                                ? match.Groups[1].Captures[0].Value
                                                : null;
                                },
                                messagePrefix,
                                browserReadyTimeoutMs);

        if (proc is null || line is null)
            throw new Exception($"Failed to launch chrome");

        string con_str = await ExtractConnUrl(line, _logger);

        _logger.LogInformation($"{messagePrefix} launching proxy for {con_str}");

        var proxy = new DebuggerProxy(loggerFactory, null, loggerId: Id);
        var browserUri = new Uri(con_str);
        WebSocket? ideSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        await proxy.Run(browserUri, ideSocket).ConfigureAwait(false);
    }

    public override void Dispose()
    {
        if (_isDisposed)
            return;

        if (_process is not null && _process.HasExited != true)
        {
            _process.CancelErrorRead();
            _process.CancelOutputRead();
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit();
            _process.Close();

            _process = null;
        }

        if (_ideWebSocket is not null)
        {
            _ideWebSocket.Abort();
            _ideWebSocket.Dispose();
            _ideWebSocket = null;
        }

        _isDisposed = true;
    }

    private async Task<string> ExtractConnUrl (string str, ILogger logger)
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
