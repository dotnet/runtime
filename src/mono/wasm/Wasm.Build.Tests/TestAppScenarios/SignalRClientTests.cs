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

public class SignalRClientTests : SignalRTestsBase
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
        BuildAspNetCoreServingWASM(config: config,
            assetName: "WasmBasicTestApp",
            projectDirSuffix: "App",
            // publish WASM App to Server's directory and avoid loading .dat files that require integrity hash calculation
            clientPublisExtraArgs: "-o ../Server/publish -p:WasmEnableThreads=true -p:InvariantGlobalization=true -p:SkipLazyLoadingTest=true",
            assertAppBundle: false, // published files are in non-standard location so assert would fail
            generatedProjectNamePrefix: "SignalRClientTests",
            runtimeType: RuntimeVariant.MultiThreaded);

        var result = await RunSdkStyleAppForBuild(new(
            Configuration: config,
            TestScenario: "SignalRClientTests",
            BrowserQueryString: new Dictionary<string, string> { ["transport"] = transport, ["message"] = "ping" } ));

        string testOutput = string.Join("\n", result.TestOutput) ?? "";
        Assert.NotEmpty(testOutput);
        // check sending and receiving threadId
        string threadIdUsedForSending = GetThreadOfAction(testOutput, @"SignalRPassMessages was sent by CurrentManagedThreadId=(\d+)", "signalR message was sent");
        string threadIdUsedForReceiving = GetThreadOfAction(testOutput, @"ReceiveMessage from server on CurrentManagedThreadId=(\d+)", "signalR message was received");
        string consoleOutput = string.Join("\n", result.ConsoleOutput);
        Assert.True("1" != threadIdUsedForSending || "1" != threadIdUsedForReceiving,
            $"Expected to send/receive with signalR in non-UI threads, instead only CurrentManagedThreadId=1 was used. ConsoleOutput: {consoleOutput}.");
    }
}
