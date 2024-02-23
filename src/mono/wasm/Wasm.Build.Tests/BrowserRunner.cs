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
using Xunit.Abstractions;

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
    private readonly ITestOutputHelper _testOutput;

    public BrowserRunner(ITestOutputHelper testOutput) => _testOutput = testOutput;

    public async Task<string> StartServerAndGetUrlAsync(
        ToolCommand cmd,
        string args,
        Action<string>? onServerMessage = null
    ) {
        TaskCompletionSource<string> urlAvailable = new();
        Action<string?> outputHandler = msg =>
        {
            if (string.IsNullOrEmpty(msg))
                return;

            onServerMessage?.Invoke(msg);

            lock (OutputLines)
            {
                OutputLines.Add(msg);
            }

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
                _exited.TrySetResult(int.Parse(m.Groups["exitCode"].Value));
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
            throw new Exception("Timed out waiting for the web server url");

        RunTask = runTask;
        return urlAvailable.Task.Result;
    }

    public async Task<IBrowser> SpawnBrowserAsync(
        string browserUrl,
        bool headless = true
    ) {
        var url = new Uri(browserUrl);
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        string[] chromeArgs = new[] { $"--explicitly-allowed-ports={url.Port}"};
        _testOutput.WriteLine($"Launching chrome ('{s_chromePath.Value}') via playwright with args = {string.Join(',', chromeArgs)}");
        return Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions{
            ExecutablePath = s_chromePath.Value,
            Headless = headless,
            Args = chromeArgs
        });
    }

    // FIXME: options
    public async Task<IPage> RunAsync(
        ToolCommand cmd,
        string args,
        bool headless = true,
        Action<IConsoleMessage>? onConsoleMessage = null,
        Action<string>? onServerMessage = null,
        Action<string>? onError = null,
        Func<string, string>? modifyBrowserUrl = null)
    {
        var urlString = await StartServerAndGetUrlAsync(cmd, args, onServerMessage);
        var browser = await SpawnBrowserAsync(urlString, headless);
        var context = await browser.NewContextAsync();
        return await RunAsync(context, urlString, headless, onConsoleMessage, onError, modifyBrowserUrl);
    }

    public async Task<IPage> RunAsync(
        IBrowserContext context,
        string browserUrl,
        bool headless = true,
        Action<IConsoleMessage>? onConsoleMessage = null,
        Action<string>? onError = null,
        Func<string, string>? modifyBrowserUrl = null,
        bool resetExitedState = false
    ) {
        if (resetExitedState)
            _exited = new ();

        if (modifyBrowserUrl != null)
            browserUrl = modifyBrowserUrl(browserUrl);

        IPage page = await context.NewPageAsync();
        if (onConsoleMessage is not null)
            page.Console += (_, msg) => onConsoleMessage(msg);

        onError ??= _testOutput.WriteLine;
        if (onError is not null)
        {
            page.PageError += (_, msg) => onError($"PageError: {msg}");
            page.Crash += (_, msg) => onError($"Crash: {msg}");
            page.FrameDetached += (_, msg) => onError($"FrameDetached: {msg}");
        }

        await page.GotoAsync(browserUrl);
        return page;
    }

    public async Task WaitForExitMessageAsync(TimeSpan timeout)
    {
        if (RunTask is null || RunTask.IsCompleted)
            throw new Exception($"No run task, or already completed");

        await Task.WhenAny(RunTask!, _exited.Task, Task.Delay(timeout));
        if (_exited.Task.IsCompleted)
        {
            _testOutput.WriteLine ($"Exited with {await _exited.Task}");
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
            _testOutput.WriteLine ($"Exited with {(await RunTask).ExitCode}");
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
