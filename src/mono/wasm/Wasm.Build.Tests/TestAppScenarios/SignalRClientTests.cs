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

    // [ConditionalTheory(typeof(BuildTestBase), nameof(IsWorkloadWithMultiThreadingForDefaultFramework))]
    [Theory]
    [InlineData("Debug", "LongPolling")]
    // [InlineData("Release", "LongPolling")]
    // [InlineData("Debug", "WebSockets")]
    // [InlineData("Release", "WebSockets")]
    public async Task SignalRPassMessages(string config, string transport)
    {
        // maybe we can hide the publish step in a AppTestBase method
        CopyTestAsset("WasmBasicTestApp", "SignalRClientTests");
        Console.WriteLine($"WASM _projectDir={_projectDir}");
        // publish WASM App to Server's directory
        // avoid loading .dat files that require integrity hash calculation
        // try fixing System.InvalidOperationException: JsonSerializerIsReflectionDisabled on message passing
        PublishProject(configuration: config, // ToDo: remove workingDirectory changes
            runtimeType: RuntimeVariant.MultiThreaded,
            assertAppBundle: false, // publish files are in non-Standard location
            extraArgs: "-o ../Server/publish -p:WasmEnableThreads=true -p:InvariantGlobalization=true -p:SkipLazyLoadingTest=true" );

        // app that will be running is in "../Server" dir
        string? parentDirName = Directory.GetParent(_projectDir!)!.FullName;
        if (parentDirName is null)
            throw new Exception("parentDirName cannot be null");
        _projectDir = Path.Combine(parentDirName, "Server");
        Console.WriteLine($"ASPNETCORE _projectDir={_projectDir}");

        // build server project
        BuildProject(configuration: config,
            // runtimeType: RuntimeVariant.MultiThreaded,
            assertAppBundle: false); // should we asset app bunlde?
        Console.WriteLine($"Starting SignalRPassMessages test with config={config} and transport={transport}");
        try
        {
            var result = await RunSdkStyleAppForBuild(new(
            Configuration: config,
            TestScenario: "SignalRClientTests", // ToDo: serverport should not be a fixed value
            BrowserQueryString: new Dictionary<string, string> { ["transport"] = transport, ["message"] = "ping" },
            OnConsoleMessage: async (page, msg) =>
            {
                Console.WriteLine($"MESSAGE -> {msg.Text}");
                _testOutput.WriteLine(msg.Text);
                if (msg.Text.Contains("Buttons added to the body, the test can be started."))
                {
                    Console.WriteLine($"clicking startconnection button");
                    await page.Locator("#startconnection").ClickAsync();
                }
                if (msg.Text.Contains("SignalR connected"))
                {
                    Console.WriteLine($"clicking sendmessage button");
                    await page.Locator("#sendmessage").ClickAsync();
                }
                if (msg.Text.Contains("ReceiveMessage from server"))
                {
                    Console.WriteLine($"clicking exitProgram button");
                    await page.Locator("#exitProgram").ClickAsync();
                }
            }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex}");
        }
        
    }
}
