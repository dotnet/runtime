
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

#nullable enable

namespace DebuggerTests;

internal abstract class WasmHostProvider : IDisposable
{
    protected ILogger _logger;
    public string Id { get; init; }
    protected Process? _process;
    protected bool _isDisposed;
    protected bool _isDisposing;

    public WasmHostProvider(string id, ILogger logger)
    {
        Id = id;
        _logger = logger;
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

    protected async Task<string?> LaunchHostAsync(ProcessStartInfo psi,
                                        HttpContext context,
                                        Func<string?, string?> checkBrowserReady,
                                        string messagePrefix,
                                        int hostReadyTimeoutMs,
                                        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(psi);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(checkBrowserReady);

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return null;
        }

        var browserReadyTCS = new TaskCompletionSource<string>();

        _logger.LogDebug($"[{Id}] Starting {psi.FileName} with {psi.Arguments}");
        _process = Process.Start(psi);
        if (_process is null)
            return null;

        Task waitForExitTask = _process.WaitForExitAsync(token);
        _process.ErrorDataReceived += (sender, e) => ProcessOutput($"{messagePrefix} browser-stderr ", e?.Data);
        _process.OutputDataReceived += (sender, e) => ProcessOutput($"{messagePrefix} browser-stdout ", e?.Data);

        _process.BeginErrorReadLine();
        _process.BeginOutputReadLine();

        Task completedTask = await Task.WhenAny(browserReadyTCS.Task, waitForExitTask, Task.Delay(hostReadyTimeoutMs))
                                        .ConfigureAwait(false);
        if (_process.HasExited)
            throw new IOException($"Process for {psi.FileName} unexpectedly exited with {_process.ExitCode} during startup.");

        if (completedTask == browserReadyTCS.Task)
        {
            _process.Exited += (_, _) =>
            {
                Console.WriteLine ($"**Browser died!**");
                Dispose();
            };

            return await browserReadyTCS.Task;
        }

        // FIXME: use custom exception types
        throw new IOException($"{messagePrefix} Timed out after {hostReadyTimeoutMs/1000}s waiting for the browser to be ready: {psi.FileName}");

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
    {
        if (_process is not null && !_process.HasExited)
        {
            _process.CancelErrorRead();
            _process.CancelOutputRead();
            _process.Kill();
            _process.WaitForExit();
            _process.Close();

            _process = null;
        }
    }

    protected static string GetBrowserPath(IEnumerable<string> pathsToProbe)
    {
        string? browserPath = FindBrowserPath();
        if (!string.IsNullOrEmpty(browserPath))
            return Path.GetFullPath(browserPath);

        throw new Exception("Could not find an installed chrome to use");

        string? FindBrowserPath()
        {
            string? _browserPath_env_var = Environment.GetEnvironmentVariable("BROWSER_PATH_FOR_DEBUGGER_TESTS");
            if (!string.IsNullOrEmpty(_browserPath_env_var))
            {
                if (File.Exists(_browserPath_env_var))
                    return _browserPath_env_var;

                Console.WriteLine ($"warning: Could not find BROWSER_PATH_FOR_DEBUGGER_TESTS={_browserPath_env_var}");
            }

            // Look for a browser installed in artifacts, for local runs
            return pathsToProbe.FirstOrDefault(p => File.Exists(p));
        }
    }
}
