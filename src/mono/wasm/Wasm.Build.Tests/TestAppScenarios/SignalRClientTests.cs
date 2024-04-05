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
        CopyTestAsset("WasmBasicTestApp", "SignalRClientTests");
        // string projectFile = Path.Combine(_projectDir!, "WasmBasicTestApp.csproj");
        // AddItemsPropertiesToProject(projectFile, "<WasmEnableThreads>true</WasmEnableThreads>");
        BuildProject(configuration: config
            // runtimeType: RuntimeVariant.MultiThreaded,
            );
        // build server project
        string serverPath = Path.Combine(Directory.GetParent(_projectDir!)!.FullName, "Server");
        BuildProject(configuration: config,
            workingDirectory: serverPath!,
            // runtimeType: RuntimeVariant.MultiThreaded,
            assertAppBundle: false
            );
        Console.WriteLine($"_projectDir={_projectDir}");
        Console.WriteLine($"Starting SignalRPassMessages test with config={config} and transport={transport}");
        try
        {
            var result = await RunSdkStyleAppForBuild(new(
            Configuration: config,
            TestScenario: "SignalRClientTests",
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
