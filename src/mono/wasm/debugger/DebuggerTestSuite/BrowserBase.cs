
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DebuggerTests;

internal abstract class BrowserBase
{
    protected ILogger logger { get; init; }

    public BrowserBase(ILogger logger) => this.logger = logger;

    protected ProcessStartInfo GetProcessStartInfo(string browserPath, string arguments, string url)
        => new()
        {
            Arguments = $"{arguments} {url}",
            UseShellExecute = false,
            FileName = browserPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

    public virtual Task Launch(HttpContext context,
                               string browserPath,
                               string url,
                               int remoteDebuggingPort,
                               string test_id,
                               string message_prefix,
                               int browser_ready_timeout_ms = 20000)
        => Task.CompletedTask;

    protected async Task<(Process, string)> LaunchBrowser(ProcessStartInfo psi,
                                        HttpContext context,
                                        Func<string, string> checkBrowserReady,
                                        string message_prefix,
                                        int browser_ready_timeout_ms)
    {

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return (null, null);
        }

        var browserReadyTCS = new TaskCompletionSource<string>();

        logger.LogDebug($"Starting {psi.FileName} with {psi.Arguments}");
        var proc = Process.Start(psi);
        await Task.Delay(1000);
        try
        {
            proc.ErrorDataReceived += (sender, e) => ProcessOutput($"{message_prefix} browser-stderr ", e.Data);
            proc.OutputDataReceived += (sender, e) => ProcessOutput($"{message_prefix} browser-stdout ", e.Data);

            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
            if (await Task.WhenAny(browserReadyTCS.Task, Task.Delay(browser_ready_timeout_ms)) != browserReadyTCS.Task)
            {
                logger.LogError($"{message_prefix} Timed out after {browser_ready_timeout_ms/1000}s waiting for the browser to be ready: {psi.FileName}");
                return (proc, null);
            }
            logger.LogInformation($"it completed: {browserReadyTCS.Task.Status}");

            return (proc, await browserReadyTCS.Task);
        }
        catch (Exception e)
        {
            logger.LogDebug($"{message_prefix} got exception {e}");
            throw;
        }

        void ProcessOutput(string prefix, string msg)
        {
            logger.LogDebug($"{prefix}{msg}");

            if (string.IsNullOrEmpty(msg) || browserReadyTCS.Task.IsCompleted)
                return;

            string result = checkBrowserReady(msg);
            if (result is not null)
                browserReadyTCS.TrySetResult(result);
        }
    }

}
