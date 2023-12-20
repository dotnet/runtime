// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;
using Wasm.Tests.Internal;

#nullable enable

namespace DebuggerTests;

internal class FirefoxProvider : WasmHostProvider
{
    private WebSocket? _ideWebSocket;
    private FirefoxDebuggerProxy? _firefoxDebuggerProxy;
    private static readonly Lazy<string> s_browserPath = new(() =>
    {
        string artifactsBinDir = Path.Combine(Path.GetDirectoryName(typeof(ChromeProvider).Assembly.Location)!, "..", "..", "..");
        return BrowserLocator.FindFirefox(artifactsBinDir, "BROWSER_PATH_FOR_TESTS");
    });

    public FirefoxProvider(string id, ILogger logger) : base(id, logger)
    {
    }

    public async Task StartBrowserAndProxyAsync(HttpContext context,
                                                string targetUrl,
                                                int remoteDebuggingPort,
                                                int proxyPort,
                                                string messagePrefix,
                                                ILoggerFactory loggerFactory,
                                                CancellationTokenSource cts,
                                                int browserReadyTimeoutMs = 20000,
                                                string locale = "en-US")
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(FirefoxProvider));

        try
        {
            string args = $"-profile {GetProfilePath(Id)} -headless -new-instance -private -start-debugger-server {remoteDebuggingPort} -UILocale {locale}";
            ProcessStartInfo? psi = GetProcessStartInfo(s_browserPath.Value, args, targetUrl);
            string? line = await LaunchHostAsync(
                                    psi,
                                    context,
                                    str =>
                                    {
                                        if (str?.Contains("Started devtools server on ") == true)
                                            return $"http://localhost:{remoteDebuggingPort}";

                                        return null;
                                    },
                                    messagePrefix,
                                    browserReadyTimeoutMs,
                                    cts.Token).ConfigureAwait(false);

            if (_process is null || line is null)
                throw new Exception($"Failed to launch firefox");
        }
        catch (Exception ex)
        {
            TestHarnessProxy.RegisterProxyExitState(Id, new(RunLoopStopReason.Exception, ex));
            throw;
        }

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
        _ = _ideWebSocket.ReceiveAsync(buff, cts.Token)
                    .ContinueWith(t =>
                    {
                        // client has closed the webserver connection, Or
                        // it has been cancelled.
                        // so, we should kill the proxy, and firefox
                        Dispose();
                    }, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.RunContinuationsAsynchronously)
                    .ConfigureAwait(false);

        _firefoxDebuggerProxy = new FirefoxDebuggerProxy();
        TestHarnessProxy.RegisterNewProxy(Id, _firefoxDebuggerProxy);
        await _firefoxDebuggerProxy
                .RunForTests(remoteDebuggingPort, proxyPort, Id, loggerFactory, _logger, cts)
                .ConfigureAwait(false);
    }

    public override void Dispose()
    {
        if (_isDisposed || _isDisposing)
            return;

        _isDisposing = true;
        if (_process?.HasExited == true)
            _firefoxDebuggerProxy?.Fail(new Exception($"Firefox unexpectedly exited with code {_process.ExitCode}"));
        else
            _firefoxDebuggerProxy?.Shutdown();

        base.Dispose();

        _logger.LogDebug($"[test_id: {Id}] {nameof(FirefoxProvider)} Dispose");

        if (_ideWebSocket is not null)
        {
            _ideWebSocket.Abort();
            _ideWebSocket.Dispose();
            _ideWebSocket = null;
        }

        _logger.LogDebug($"[test_id: {Id}] {nameof(FirefoxProvider)} Dispose done");
        _isDisposed = true;
        _isDisposing = false;
    }

    private static string GetProfilePath(string Id)
    {
        string prefs = """
            user_pref("devtools.chrome.enabled", true);
            user_pref("devtools.debugger.remote-enabled", true);
            user_pref("devtools.debugger.prompt-connection", false);
            user_pref("devtools.console.stdout.content", true);
            user_pref("browser.dom.window.dump.enabled", true);
            """;

        string profilePath = Path.GetFullPath(Path.Combine(DebuggerTestBase.DebuggerTestAppPath, $"test-profile-{Id}"));
        if (Directory.Exists(profilePath))
            Directory.Delete(profilePath, recursive: true);

        Directory.CreateDirectory(profilePath);
        File.WriteAllText(Path.Combine(profilePath, "prefs.js"), prefs);

        return profilePath;
    }
}
