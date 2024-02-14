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
        RunOptions options = new(
            Configuration: config,
            TestScenario: "SignalRPassMessages",
            BrowserQueryString: new Dictionary<string, string> { ["message"] = "test",  ["transport"] = transport },
            ExtraArgs: $"--logRootPath {s_buildEnv.LogRootPath}"
        );

        string rootProjectPath = Directory.GetParent(_projectDir!)?.FullName ?? "";
        string clientProjectDir = Path.Combine(rootProjectPath, "BlazorHosted.Client");
        string frameworkDir = FindBlazorBinFrameworkDir(config, forPublish: false, projectDir: clientProjectDir);
        BuildProject(
            configuration: config,
            binFrameworkDir: frameworkDir,
            runtimeType: RuntimeVariant.MultiThreaded);

        var result = await RunSdkStyleAppForBuild(options);

        // make sure we're not in the main thread (id != 1)
        var confirmation = result.TestOutput.FirstOrDefault(m => m.Contains($"[{transport}] Client confirms receiving message=test CurrentManagedThreadId="));
        Assert.NotNull(confirmation);
        string currentManagedThreadId = confirmation.Split("CurrentManagedThreadId=")[1];
        Assert.NotEqual("1", currentManagedThreadId);
    }
}
