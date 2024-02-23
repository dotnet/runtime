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

        string rootProjectPath = Directory.GetParent(_projectDir!)?.FullName ?? "";
        string clientProjectDir = Path.Combine(rootProjectPath, "BlazorHosted.Client");
        string frameworkDir = FindBlazorBinFrameworkDir(config, forPublish: false, projectDir: clientProjectDir);
        BuildProject(
            configuration: config,
            binFrameworkDir: frameworkDir,
            runtimeType: RuntimeVariant.MultiThreaded);

        using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!);
        await using var runner = new BrowserRunner(_testOutput);
        var url = await runner.StartServerAndGetUrlAsync(runCommand, $"run -c {config} --no-build");
        IBrowser browser = await runner.SpawnBrowserAsync(url);
        IBrowserContext context = await browser.NewContextAsync();
        List<string> testOutput = new();

        var chatUrl = url + $"/chat?transport={transport}&message=ping";
        testOutput.Add($"Starting to run on browser URL: {chatUrl}");
        var page = await runner.RunAsync(context, chatUrl);
        #pragma warning disable 4014
        page.Console += (_, msg) => OnConsoleMessage(msg);
        #pragma warning restore 4014
        testOutput.Add($"Wait for loading state");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.ClickAsync("button#connectButton");
        // give the connection some time to establish
        await Task.Delay(3000);

        async Task OnConsoleMessage(IConsoleMessage msg)
        {
            if (msg.Text.Contains("TestOutput ->"))
            {
                testOutput.Add(msg.Text);
            }

            if (msg.Text.Contains("SignalR connected"))
                await page.ClickAsync("button#subscribeButton");

            if (msg.Text.Contains("Subscribed to ReceiveMessage"))
                await page.ClickAsync("button#sendMessageButton");

            if (msg.Text.Contains("ReceiveMessage from server"))
                await page.ClickAsync("button#disconnectButton");

            if (msg.Text.Contains("SignalR got disconnected"))
                await page.ClickAsync("button#killServerButton");

            if (msg.Text.Contains("Exit signal was sent"))
            {
                await runner.WaitForExitMessageAsync(TimeSpan.FromSeconds(10));
            }
        }
        string output = string.Join(Environment.NewLine, testOutput);

        // check sending threadId
        var confirmation = testOutput.FirstOrDefault(m => m.Contains($"SignalRPassMessages was sent by CurrentManagedThreadId="));
        Assert.True(confirmation != null, $"Expected to find a log that signalR message was sent. TestOutput: {output}.");
        string threadIdUsedForSending = confirmation?.Split("CurrentManagedThreadId=")[1] ?? "";

        // check receiving threadId
        confirmation = testOutput.FirstOrDefault(m => m.Contains($"ReceiveMessage from server on CurrentManagedThreadId="));
        Assert.True(confirmation != null, $"Expected to find a log that signalR message was received. TestOutput: {output}.");
        string threadIdUsedForReceiving = confirmation?.Split("CurrentManagedThreadId=")[1] ?? "";

        Assert.True("1" != threadIdUsedForSending || "1" != threadIdUsedForReceiving,
            $"Expected to send/receive with signalR in non-UI threads, instead only CurrentManagedThreadId=1 was used. TestOutput: {output}.");
    }
}
