// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class SignalRClientTests : AppTestBase
{
    public SignalRClientTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsWorkloadWithMultiThreadingForDefaultFramework))]
    [InlineData("Debug", "LongPolling")]
    [InlineData("Release", "LongPolling")]
    [InlineData("Debug", "WebSockets")]
    [InlineData("Release", "WebSockets")]
    public async Task SignalRPassMessages(string config, string transport)
    {
        CopyTestAsset("BlazorHostedApp", "SignalRClientTests");
        string frameworkDir = FindBlazorHostedBinFrameworkDir(config,
            forPublish: false,
            clientDirRelativeToProjectDir: "../BlazorHosted.Client");
        BuildProject(configuration: config,
            binFrameworkDir: frameworkDir,
            runtimeType: RuntimeVariant.MultiThreaded);

        List<string> consoleOutput = new();
        List<string> serverOutput = new();

        // We are using build (not publish),
        // we need to instruct static web assets to use manifest file,
        // because wwwroot in bin doesn't contain all files (for build)
        s_buildEnv.EnvVars["ASPNETCORE_ENVIRONMENT"] = "Development";
        using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!);
        await using var runner = new BrowserRunner(_testOutput);
        var url = await runner.StartServerAndGetUrlAsync(
            cmd: runCommand,
            args: $"run -c {config} --no-build",
            onServerMessage: OnServerMessage);
        var chatUrl = url + $"/chat?transport={transport}&message=ping";
        IBrowser browser = await runner.SpawnBrowserAsync(url);
        IBrowserContext context = await browser.NewContextAsync();

        var page = await runner.RunAsync(context, chatUrl);
        try
        {
            #pragma warning disable 4014
            page.Console += (_, msg) => OnConsoleMessage(msg);
            #pragma warning restore 4014
            _testOutput.WriteLine($"Wait for loading state");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await page.ClickAsync("button#connectButton");
            // give the connection some time to establish
            await Task.Delay(3000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex}");
            Console.WriteLine($"ConsoleOutput: {string.Join(Environment.NewLine, consoleOutput)}");
            Console.WriteLine($"ServerOutput: {string.Join(Environment.NewLine, serverOutput)}");
            throw;
        }

        async Task OnConsoleMessage(IConsoleMessage msg)
        {
            consoleOutput.Add(msg.Text);
            if (msg.Text.Contains("TestOutput ->"))
            {
                _testOutput.WriteLine(msg.Text);
            }

            if (msg.Text.Contains("SignalR connected"))
                await page.ClickAsync("button#subscribeButton");

            if (msg.Text.Contains("Subscribed to ReceiveMessage"))
                await page.ClickAsync("button#sendMessageButton");

            if (msg.Text.Contains("ReceiveMessage from server"))
                await page.ClickAsync("button#exitProgramButton");

            if (msg.Text.Contains("Exit signal was sent"))
            {
                await runner.WaitForExitMessageAsync(TimeSpan.FromSeconds(10));
            }
        }

        void OnServerMessage(string msg) => serverOutput.Add(msg);

        // check sending threadId
        string output = _testOutput.ToString() ?? "";
        Assert.NotEmpty(output);
        Match match = Regex.Match(output, @"SignalRPassMessages was sent by CurrentManagedThreadId=(\d+)");
        Assert.True(match.Success, $"Expected to find a log that signalR message was sent. TestOutput: {output}.");
        string threadIdUsedForSending = match.Groups[1].Value ?? "";

        // check receiving threadId
        match = Regex.Match(output, @"ReceiveMessage from server on CurrentManagedThreadId=(\d+)");
        Assert.True(match.Success, $"Expected to find a log that signalR message was sent. TestOutput: {output}.");
        string threadIdUsedForReceiving = match.Groups[1].Value ?? "";

        Assert.True("1" != threadIdUsedForSending || "1" != threadIdUsedForReceiving,
            $"Expected to send/receive with signalR in non-UI threads, instead only CurrentManagedThreadId=1 was used. TestOutput: {output}.");
    }
}
