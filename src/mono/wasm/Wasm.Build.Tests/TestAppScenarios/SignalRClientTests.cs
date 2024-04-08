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
    [ActiveIssue("https://github.com/dotnet/runtime/issues/100445")] // to be fixed by: "https://github.com/dotnet/aspnetcore/issues/54365"
    [InlineData("Debug", "LongPolling")]
    [InlineData("Release", "LongPolling")]
    [InlineData("Debug", "WebSockets")]
    [InlineData("Release", "WebSockets")]
    public async Task SignalRPassMessages(string config, string transport)
    {
        BlazorHostedBuild(config,
            assetName: "BlazorHostedApp",
            clientDirRelativeToProjectDir: "../BlazorHosted.Client",
            generatedProjectNamePrefix: "SignalRClientTests",
            runtimeType: RuntimeVariant.MultiThreaded);

        List<string> consoleOutput = new();
        List<string> serverOutput = new();

        var result = await RunSdkStyleAppForBuild(new(
            Configuration: config,
            // We are using build (not publish),
            // we need to instruct static web assets to use manifest file,
            // because wwwroot in bin doesn't contain all files (for build)
            ServerEnvironment: new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Development" },
            BrowserPath: "/chat",
            BrowserQueryString: new Dictionary<string, string> { ["transport"] = transport, ["message"] = "ping" },
            OnServerMessage: (msg) => serverOutput.Add(msg),
            OnConsoleMessage: async (page, msg) =>
            {
                consoleOutput.Add(msg.Text);
                if (msg.Text.Contains("TestOutput ->"))
                    _testOutput.WriteLine(msg.Text);

                // prevent timeouts with [Long Running Test] on error
                if (msg.Text.ToLowerInvariant().Contains("error"))
                {
                    Console.WriteLine(msg.Text);
                    Console.WriteLine(_testOutput);
                    throw new Exception(msg.Text);
                }

                if (msg.Text.Contains("Finished GetQueryParameters"))
                    await SaveClickButtonAsync(page, "button#connectButton");

                if (msg.Text.Contains("SignalR connected"))
                    await SaveClickButtonAsync(page, "button#subscribeButton");

                if (msg.Text.Contains("Subscribed to ReceiveMessage"))
                    await SaveClickButtonAsync(page, "button#sendMessageButton");

                if (msg.Text.Contains("ReceiveMessage from server"))
                    await SaveClickButtonAsync(page, "button#exitProgramButton");
            }
        ));

        string output = _testOutput.ToString() ?? "";
        Assert.NotEmpty(output);
        // check sending and receiving threadId
        string threadIdUsedForSending = GetThreadOfAction(output, @"SignalRPassMessages was sent by CurrentManagedThreadId=(\d+)", "signalR message was sent");
        string threadIdUsedForReceiving = GetThreadOfAction(output, @"ReceiveMessage from server on CurrentManagedThreadId=(\d+)", "signalR message was received");
        Assert.True("1" != threadIdUsedForSending || "1" != threadIdUsedForReceiving,
            $"Expected to send/receive with signalR in non-UI threads, instead only CurrentManagedThreadId=1 was used. TestOutput: {output}.");
    }

    private string GetThreadOfAction(string testOutput, string pattern, string actionDescription)
    {
        Match match = Regex.Match(testOutput, pattern);
        Assert.True(match.Success, $"Expected to find a log that {actionDescription}. TestOutput: {testOutput}.");
        return match.Groups[1].Value ?? "";
    }

    private async Task SaveClickButtonAsync(IPage page, string selector)
    {
        await page.WaitForSelectorAsync(selector);
        await page.ClickAsync(selector);
    }
}
