// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;

#nullable enable

namespace DebuggerTests;

internal class FirefoxProvider : WasmHostProvider
{
    private Process? _process;
    private WebSocket? _ideWebSocket;
    private FirefoxProxyServer? _firefoxProxyServer;
    private bool _isDisposed;

    public FirefoxProvider(string id, ILogger logger) : base(id, logger)
    {
    }

    public async Task StartBrowserAndProxy(HttpContext context,
                                      string browserPath,
                                      string targetUrl,
                                      int remoteDebuggingPort,
                                      int proxyPort,
                                      string messagePrefix,
                                      ILoggerFactory loggerFactory,
                                      int browserReadyTimeoutMs = 20000)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(FirefoxProvider));

        string args = $"-profile {GetProfilePath(Id)} -headless -new-instance -private -start-debugger-server {remoteDebuggingPort}";
        ProcessStartInfo? psi = GetProcessStartInfo(browserPath, args, targetUrl);
        (_process, string? line) = await LaunchHost(
                                psi,
                                context,
                                str =>
                                {
                                    // FIXME: instead of this, we can wait for the port to open
                                    //for running debugger tests on firefox
                                    if (str?.Contains("[GFX1-]: RenderCompositorSWGL failed mapping default framebuffer, no dt") == true)
                                        return $"http://localhost:{remoteDebuggingPort}";

                                    return null;
                                },
                                messagePrefix,
                                browserReadyTimeoutMs);

        if (_process is null || line is null)
            throw new Exception($"Failed to launch firefox");

        /*
         * Firefox uses a plain tcp connection, so we use that for communicating
         * with the browser. But the tests connect to the webserver via a websocket,
         * so we *accept* that here to complete the connection.
         *
         * Normally, when the tests are closing down, they close that webserver
         * connection, and the proxy would shutdown too. But in this case, we need
         * to explicitly trigger the proxy/browser shutdown when the websocket
         * is closed.
         */
        _ideWebSocket = await context.WebSockets.AcceptWebSocketAsync();

        ArraySegment<byte> buff = new(new byte[10]);
        CancellationToken token = new();
        _ = _ideWebSocket.ReceiveAsync(buff, token)
                    .ContinueWith(t =>
                    {
                        Console.WriteLine ($"[{Id}] firefox provider - ide connection closed");
                        // client has closed the webserver connection, Or
                        // it has been cancelled.
                        // so, we should kill the proxy, and firefox
                        Dispose();
                    }, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.RunContinuationsAsynchronously)
                    .ConfigureAwait(false);

        _firefoxProxyServer = new FirefoxProxyServer();
        await _firefoxProxyServer
                .RunForTests(remoteDebuggingPort, proxyPort, Id, loggerFactory, _logger)
                .ConfigureAwait(false);
    }

    public override void Dispose()
    {
        if (_isDisposed)
            return;

        _firefoxProxyServer?.Shutdown();

        _logger.LogDebug($"[{Id}] {nameof(FirefoxProvider)} Dispose");
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

        _logger.LogDebug($"[{Id}] {nameof(FirefoxProvider)} Dispose done");
        _isDisposed = true;
    }

    private static string GetProfilePath(string Id)
    {
        string prefs = @"
        user_pref(""devtools.chrome.enabled"", true);
        user_pref(""devtools.debugger.remote-enabled"", true);
        user_pref(""devtools.debugger.prompt-connection"", false);";

        string profilePath = Path.GetFullPath(Path.Combine(DebuggerTestBase.DebuggerTestAppPath, $"test-profile-{Id}"));
        if (Directory.Exists(profilePath))
            Directory.Delete(profilePath, recursive: true);

        Directory.CreateDirectory(profilePath);
        File.WriteAllText(Path.Combine(profilePath, "prefs.js"), prefs);

        return profilePath;
    }
}
