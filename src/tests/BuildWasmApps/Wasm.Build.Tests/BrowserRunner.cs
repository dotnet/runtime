// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Wasm.Tests.Internal;

namespace Wasm.Build.Tests;

internal class BrowserRunner : IAsyncDisposable
{
    private static Regex s_blazorUrlRegex = new Regex("Now listening on: (?<url>https?://.*$)");
    private static Regex s_appHostUrlRegex = new Regex("^App url: (?<url>https?://.*$)");
    private static Regex s_exitRegex = new Regex("WASM EXIT (?<exitCode>[0-9]+)$");
    private static readonly Lazy<string> s_chromePath = new(() =>
    {
        string artifactsBinDir = Path.Combine(Path.GetDirectoryName(typeof(BuildTestBase).Assembly.Location)!, "..", "..", "..", "..");
        return BrowserLocator.FindChrome(artifactsBinDir, "BROWSER_PATH_FOR_TESTS");
    });

    public IPlaywright? Playwright { get; private set; }
    public IBrowser? Browser { get; private set; }
    public Task<CommandResult>? RunTask { get; private set; }
    public IList<string> OutputLines { get; private set; } = new List<string>();
    private TaskCompletionSource<int> _exited = new();

    // FIXME: options
    public async Task<IPage> RunAsync(ToolCommand cmd, string args, bool headless = true)
    {
        TaskCompletionSource<string> urlAvailable = new();
        Action<string?> outputHandler = msg =>
        {
            if (string.IsNullOrEmpty(msg))
                return;

            OutputLines.Add(msg);

            Match m = s_appHostUrlRegex.Match(msg);
            if (!m.Success)
                m = s_blazorUrlRegex.Match(msg);

            if (m.Success)
            {
                string url = m.Groups["url"].Value;
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    urlAvailable.TrySetResult(m.Groups["url"].Value);
                return;
            }

            m = s_exitRegex.Match(msg);
            if (m.Success)
            {
                _exited.SetResult(int.Parse(m.Groups["exitCode"].Value));
                return;
            }
        };

        cmd.WithErrorDataReceived(outputHandler).WithOutputDataReceived(outputHandler);
        var runTask = cmd.ExecuteAsync(args);

        await Task.WhenAny(runTask, urlAvailable.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        if (runTask.IsCompleted)
        {
            var res = await runTask;
            res.EnsureSuccessful();

            throw new Exception($"Process ended before the url was found");
        }
        if (!urlAvailable.Task.IsCompleted)
            throw new Exception("Timed out waiting for the app host url");

        var url = new Uri(urlAvailable.Task.Result);
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions{
            ExecutablePath = s_chromePath.Value,
            Headless = headless,
            Args = OperatingSystem.IsWindows()
                        ? new[] { $"--explicitly-allowed-ports={url.Port}" }
                        : Array.Empty<string>()
        });

        IPage page = await Browser.NewPageAsync();
        await page.GotoAsync(urlAvailable.Task.Result);
        RunTask = runTask;
        return page;
    }

    public async Task WaitForExitMessageAsync(TimeSpan timeout)
    {
        if (RunTask is null || RunTask.IsCompleted)
            throw new Exception($"No run task, or already completed");

        await Task.WhenAny(RunTask!, _exited.Task, Task.Delay(timeout));
        if (_exited.Task.IsCompleted)
        {
            Console.WriteLine ($"Exited with {await _exited.Task}");
            return;
        }

        throw new Exception($"Timed out after {timeout.TotalSeconds}s waiting for 'WASM EXIT' message");
    }

    public async Task WaitForProcessExitAsync(TimeSpan timeout)
    {
        if (RunTask is null || RunTask.IsCompleted)
            throw new Exception($"No run task, or already completed");

        await Task.WhenAny(RunTask!, _exited.Task, Task.Delay(timeout));
        if (RunTask.IsCanceled)
        {
            Console.WriteLine ($"Exited with {(await RunTask).ExitCode}");
            return;
        }

        throw new Exception($"Timed out after {timeout.TotalSeconds}s waiting for process to exit");
    }

    public async ValueTask DisposeAsync()
    {
        if (Browser is not null)
            await Browser.DisposeAsync();
        Playwright?.Dispose();
    }
}
