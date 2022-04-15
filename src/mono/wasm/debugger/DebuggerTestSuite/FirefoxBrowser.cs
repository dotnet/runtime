// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;

#nullable enable

namespace DebuggerTests;

internal class FirefoxBrowser : BrowserBase
{

    public FirefoxBrowser(ILogger logger) : base(logger)
    {
    }

    public override async Task LaunchAndStartProxy(HttpContext context,
                                      string browserPath,
                                      string url,
                                      int remoteDebuggingPort,
                                      string test_id,
                                      string message_prefix,
                                      int browser_ready_timeout_ms = 20000)
    {
        string args = $"-profile {GetProfilePath()} -headless -private -start-debugger-server {remoteDebuggingPort}";
        ProcessStartInfo? psi = GetProcessStartInfo(browserPath, args, url);
        (Process? proc, string? line) = await LaunchBrowser(
                                psi,
                                context,
                                str =>
                                {
                                    //for running debugger tests on firefox
                                    if (str?.Contains("[GFX1-]: RenderCompositorSWGL failed mapping default framebuffer, no dt") == true)
                                        return $"http://localhost:{remoteDebuggingPort}";

                                    return null;
                                },
                                message_prefix,
                                browser_ready_timeout_ms);

        if (proc is null || line is null)
            throw new Exception($"Failed to launch firefox");

        string logFilePath = Path.Combine(DebuggerTestBase.TestLogPath, $"{test_id}-proxy.log");
        File.Delete(logFilePath);

        var proxyLoggerFactory = LoggerFactory.Create(
            builder => builder
                    .AddSimpleConsole(options =>
                        {
                            // options.SingleLine = true;
                            options.TimestampFormat = "[HH:mm:ss] ";
                        })
                .AddFilter(null, LogLevel.Debug))
                .AddFile(logFilePath, minimumLevel: LogLevel.Trace);

        var proxy = new DebuggerProxy(proxyLoggerFactory, null, loggerId: test_id);
        WebSocket? ideSocket = null;
        try
        {
            ideSocket = await context.WebSockets.AcceptWebSocketAsync();
            var proxyFirefox = new FirefoxProxyServer(proxyLoggerFactory, remoteDebuggingPort);
            await proxyFirefox.RunForTests(ideSocket);
        }
        catch (Exception ex)
        {
            logger.LogError($"{message_prefix} {ex}");
            ideSocket?.Abort();
            ideSocket?.Dispose();
            throw;
        }
        finally
        {
            proc.CancelErrorRead();
            proc.CancelOutputRead();
            proc.Kill();
            proc.WaitForExit();
            proc.Close();
        }
    }

    private static string GetProfilePath()
    {
        string prefs = @"
        user_pref(""devtools.chrome.enabled"", true);
        user_pref(""devtools.debugger.remote-enabled"", true);
        user_pref(""devtools.debugger.prompt-connection"", false);";

        string profilePath = Path.GetFullPath(Path.Combine(DebuggerTestBase.DebuggerTestAppPath, "test-profile"));
        if (Directory.Exists(profilePath))
            Directory.Delete(profilePath, recursive: true);

        Directory.CreateDirectory(profilePath);
        File.WriteAllText(Path.Combine(profilePath, "prefs.js"), prefs);

        return profilePath;
    }
}
