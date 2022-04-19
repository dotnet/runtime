
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

#nullable enable

namespace DebuggerTests;

internal abstract class WasmHostProvider : IDisposable
{
    protected ILogger _logger;
    public string Id { get; init; }

    public WasmHostProvider(string id, ILogger logger)
    {
        Id = id;
        this._logger = logger;
    }

    protected ProcessStartInfo GetProcessStartInfo(string browserPath, string arguments, string url)
        => new()
        {
            Arguments = $"{arguments} {url}",
            UseShellExecute = false,
            FileName = browserPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

    protected async Task<(Process?, string?)> LaunchHost(ProcessStartInfo psi!!,
                                        HttpContext context!!,
                                        Func<string?, string?> checkBrowserReady!!,
                                        string messagePrefix,
                                        int hostReadyTimeoutMs)
    {

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return (null, null);
        }

        var browserReadyTCS = new TaskCompletionSource<string>();

        _logger.LogDebug($"Starting {psi.FileName} with {psi.Arguments}");
        var proc = Process.Start(psi);
        if (proc is null)
            return (null, null);

        await Task.Delay(1000);
        try
        {
            proc.ErrorDataReceived += (sender, e) => ProcessOutput($"{messagePrefix} browser-stderr ", e?.Data);
            proc.OutputDataReceived += (sender, e) => ProcessOutput($"{messagePrefix} browser-stdout ", e?.Data);

            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
            if (await Task.WhenAny(browserReadyTCS.Task, Task.Delay(hostReadyTimeoutMs)) != browserReadyTCS.Task)
            {
                _logger.LogError($"{messagePrefix} Timed out after {hostReadyTimeoutMs/1000}s waiting for the browser to be ready: {psi.FileName}");
                return (proc, null);
            }

            return (proc, await browserReadyTCS.Task);
        }
        catch (Exception e)
        {
            _logger.LogDebug($"{messagePrefix} got exception {e}");
            throw;
        }

        void ProcessOutput(string prefix, string? msg)
        {
            _logger.LogDebug($"{prefix}{msg}");

            if (string.IsNullOrEmpty(msg) || browserReadyTCS.Task.IsCompleted)
                return;

            string? result = checkBrowserReady(msg);
            if (result is not null)
                browserReadyTCS.TrySetResult(result);
        }
    }

    public virtual void Dispose()
    {}
}
