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
using System.Threading;
using System.Collections.Generic;
using Wasm.Tests.Internal;
using System.Linq;

#nullable enable

namespace DebuggerTests;

internal class ChromeProvider : WasmHostProvider
{
    static readonly Regex s_parseConnection = new (@"listening on (ws?s://[^\s]*)");
    private WebSocket? _ideWebSocket;
    private DebuggerProxy? _debuggerProxy;
    private static readonly Lazy<string> s_browserPath = new(() =>
    {
        string artifactsBinDir = Path.Combine(Path.GetDirectoryName(typeof(ChromeProvider).Assembly.Location)!, "..", "..", "..");
        return BrowserLocator.FindChrome(artifactsBinDir, "BROWSER_PATH_FOR_TESTS");
    });
    private static readonly string[] s_messagesToFilterOut = new[]
    {
        "Received unexpected number of handles",
        "Failed to connect to the bus:",
    };

    public ChromeProvider(string id, ILogger logger) : base(id, logger)
    {
    }

    public async Task StartBrowserAndProxyAsync(HttpContext context,
                                                string targetUrl,
                                                int remoteDebuggingPort,
                                                string messagePrefix,
                                                ILoggerFactory loggerFactory,
                                                CancellationTokenSource cts,
                                                int browserReadyTimeoutMs = 20000,
                                                string locale = "en-US")
    {
        string? line;
        try
        {
            // for WIndows setting --lang arg is enough
            if (!OperatingSystem.IsWindows())
                Environment.SetEnvironmentVariable("LANGUAGE", locale);
            ProcessStartInfo psi = GetProcessStartInfo(s_browserPath.Value, GetInitParms(remoteDebuggingPort, locale), "about:blank");
            line = await LaunchHostAsync(
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
                                    browserReadyTimeoutMs,
                                    cts.Token).ConfigureAwait(false);

            if (_process is null || line is null)
                throw new Exception($"Failed to launch chrome");
        }
        catch (Exception ex)
        {
            TestHarnessProxy.RegisterProxyExitState(Id, new(RunLoopStopReason.Exception, ex));
            throw;
        }

        string con_str = await ExtractConnUrl(line, _logger);

        _logger.LogInformation($"{messagePrefix} launching proxy for {con_str}");

        _debuggerProxy = new DebuggerProxy(loggerFactory, loggerId: Id);
        TestHarnessProxy.RegisterNewProxy(Id, _debuggerProxy);
        var browserUri = new Uri(con_str);
        WebSocket? ideSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        await _debuggerProxy.Run(browserUri, ideSocket, cts).ConfigureAwait(false);
    }

    public override void Dispose()
    {
        if (_isDisposed || _isDisposing)
            return;

        _isDisposing = true;
        _debuggerProxy?.Shutdown();
        base.Dispose();

        if (_ideWebSocket is not null)
        {
            _ideWebSocket.Abort();
            _ideWebSocket.Dispose();
            _ideWebSocket = null;
        }

        _isDisposed = true;
        _isDisposing = false;
    }

    protected override bool ShouldMessageBeLogged(string prefix, string? msg)
    {
        if (msg is null || !prefix.Contains("browser-stderr"))
            return true;

        return !s_messagesToFilterOut.Any(f => msg.Contains(f));
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
            logger.LogTrace("res is {0}", res);

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

    private static string GetInitParms(int port, string lang="en-US")
    {
        string str = $"--headless --disable-gpu --lang={lang} --incognito --remote-debugging-port={port}";
        if (File.Exists("/.dockerenv"))
        {
            Console.WriteLine ("Detected a container, disabling sandboxing for debugger tests.");
            str = "--no-sandbox " + str;
        }
        return str;
    }
}
