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

    // ToDo: run only on MT
    [Fact]
    public async Task SignalRClientWorksWithLongPolling()
    {
        CopyTestAsset("BlazorHostedApp", "SignalRClientTests");

        string config = "Debug";
        RunOptions options = new(
            Configuration: config,
            // for now, TestScenario is ignored - there's only one scenario
            TestScenario: "SignalRClientTests",
            BrowserQueryString: new Dictionary<string, string> { ["message"] = "test",  ["transport"] = "LongPolling" },
            ExtraArgs: $"--logRootPath {s_buildEnv.LogRootPath}"
        );

        string rootProjectPath = Directory.GetParent(_projectDir!)?.FullName ?? "";
        string clientProjectDir = Path.Combine(rootProjectPath, "BlazorHosted.Client");
        string frameworkDir = FindBlazorBinFrameworkDir(config, forPublish: false, projectDir: clientProjectDir);
        BuildProject(
            configuration: "Debug",
            binFrameworkDir: frameworkDir,
            runtimeType: RuntimeVariant.MultiThreaded, 
            assertAppBundle: false); // Temporary fix to avoid: found dotnet.native.9.0.0-dev.u27nwa5zma.js instead of dotnet.native.9.0.0.js

        var result = await RunSdkStyleAppForBuild(options);
        foreach (var a in result.TestOutput)
        {
            Console.WriteLine($"TestOutput = {a}");
        }
        // foreach (var a in result.ConsoleOutput)
        // {
        //     Console.WriteLine($"{a}");
        // }
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal("[LongPolling] Client confirms receiving message=test from server", m)
        );

        // ToDo:
        // 1) fix error: SharedArrayBuffer is not enabled on this page
        // 2) can we reproduce the failures?
        // 3) add remaining tests from https://github.com/dotnet/aspnetcore/blob/main/src/Components/test/E2ETest/Tests/SignalRClientTest.cs
    }
}
