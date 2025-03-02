// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Linq;
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
    private static Regex s_appPublishedUrlRegex = new Regex(@"^\s{2}(?<url>https?://.*$)");
    private static readonly Regex s_payloadRegex = new Regex("\"payload\":\"(?<payload>[^\"]*)\"", RegexOptions.Compiled);
    private static Regex s_exitRegex = new Regex("WASM EXIT (?<exitCode>-?[0-9]+)$");
    private static readonly Lazy<string> s_chromePath = new(() =>
    {
        string artifactsBinDir = Path.Combine(Path.GetDirectoryName(typeof(BuildTestBase).Assembly.Location)!, "..", "..", "..", "..");
        return BrowserLocator.FindChrome(artifactsBinDir, "CHROME_PATH_FOR_TESTS");
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

            var regexes = new[] { s_appHostUrlRegex, s_blazorUrlRegex, s_appPublishedUrlRegex };
            Match m = Match.Empty;

            foreach (var regex in regexes)
            {
                m = regex.Match(msg);
                if (m.Success)
                    break;
            }

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

        var delayTask = Task.Delay(TimeSpan.FromSeconds(30));

        await Task.WhenAny(runTask, urlAvailable.Task, delayTask);
        if (delayTask.IsCompleted)
        {
            _testOutput.WriteLine("First 30s delay reached, scheduling next one");

            delayTask = Task.Delay(TimeSpan.FromSeconds(30));
            await Task.WhenAny(runTask, urlAvailable.Task, delayTask);
        }

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
        bool headless = true,
        int? timeout = null,
        int maxRetries = 3,
        string locale = "en-US"
    ) {
        var url = new Uri(browserUrl);
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        // codespaces: ignore certificate error -> Microsoft.Playwright.PlaywrightException : net::ERR_CERT_AUTHORITY_INVALID
        string[] chromeArgs = new[] { $"--explicitly-allowed-ports={url.Port}", "--ignore-certificate-errors", $"--lang={locale}" };
        if (headless)
            chromeArgs = chromeArgs.Append("--headless").ToArray();
        _testOutput.WriteLine($"Launching chrome ('{s_chromePath.Value}') via playwright with args = {string.Join(',', chromeArgs)}");

        int attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
                    ExecutablePath = s_chromePath.Value,
                    Args = chromeArgs,
                    Timeout = timeout
                });
                Browser.Disconnected += (sender, e) =>
                {
                    Browser = null;
                    _testOutput.WriteLine("Browser has been disconnected");
                };
                break;
            }
            catch (System.TimeoutException ex)
            {
                attempt++;
                _testOutput.WriteLine($"Attempt {attempt} failed with TimeoutException: {ex.Message}");
            }
        }
        if (attempt == maxRetries)
            throw new Exception($"Failed to launch browser after {maxRetries} attempts");
        return Browser!;
    }

    // FIXME: options
    public async Task<IPage> RunAsync(
        ToolCommand cmd,
        string args,
        bool headless = true,
        string locale = "en-US",
        Action<string, string>? onConsoleMessage = null,
        Action<string>? onServerMessage = null,
        Action<string>? onError = null,
        Func<string, string>? modifyBrowserUrl = null)
    {
        var urlString = await StartServerAndGetUrlAsync(cmd, args, onServerMessage);
        var browser = await SpawnBrowserAsync(urlString, headless, locale: locale);
        var context = await browser.NewContextAsync(new BrowserNewContextOptions { Locale = locale });
        return await RunAsync(context, urlString, headless, onConsoleMessage, onError, modifyBrowserUrl);
    }

    public async Task<IPage> RunAsync(
        IBrowserContext context,
        string browserUrl,
        bool headless = true,
        Action<string, string>? onConsoleMessage = null,
        Action<string>? onError = null,
        Func<string, string>? modifyBrowserUrl = null,
        bool resetExitedState = false
    ) {
        if (resetExitedState)
            _exited = new ();

        if (modifyBrowserUrl != null)
            browserUrl = modifyBrowserUrl(browserUrl);

        IPage page = await context.NewPageAsync();

        page.Console += (_, msg) =>
        {
            string message = msg.Text;
            Match payloadMatch = s_payloadRegex.Match(message);
            if (payloadMatch.Success)
            {
                message = payloadMatch.Groups["payload"].Value;
            }
            Match exitMatch = s_exitRegex.Match(message);
            if (exitMatch.Success)
            {
                int exitCode = int.Parse(exitMatch.Groups["exitCode"].Value);
                _exited.TrySetResult(exitCode);
            }
            if (onConsoleMessage is not null)
            {
                onConsoleMessage(msg.Type, message);
            }
        };

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

    public async Task<int> WaitForExitMessageAsync(TimeSpan timeout)
    {
        if (RunTask is null || RunTask.IsCompleted)
            throw new Exception($"No run task, or already completed");

        await Task.WhenAny(RunTask!, _exited.Task, Task.Delay(timeout));
        if (_exited.Task.IsCompleted)
        {
            int code = await _exited.Task;
            _testOutput.WriteLine ($"Exited with {code}");
            return code;
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
        try
        {
            if (Browser is not null)
            {
                await Browser.DisposeAsync();
                Browser = null;
            }
        }
        catch (PlaywrightException ex)
        {
            _testOutput.WriteLine($"PlaywrightException occurred during DisposeAsync: {ex.Message}");
        }
        finally
        {
            Playwright?.Dispose();
        }
    }
}
