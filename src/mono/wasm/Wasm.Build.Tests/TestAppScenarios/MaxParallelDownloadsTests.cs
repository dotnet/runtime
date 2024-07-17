// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;
using System.Text.RegularExpressions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class MaxParallelDownloadsTests : AppTestBase
{
    public MaxParallelDownloadsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Release", "1")]
    [InlineData("Release", "4")]
    public async Task NeverFetchMoreThanMaxAllowed(string config, string maxParallelDownloads)
    {
        CopyTestAsset("WasmBasicTestApp", "MaxParallelDownloadsTests", "App");
        BuildProject(config);

        var result = await RunSdkStyleAppForBuild(new(
            Configuration: config,
            TestScenario: "MaxParallelDownloads",
            BrowserQueryString:  new Dictionary<string, string> { ["maxParallelDownloads"] = maxParallelDownloads }
        ));
        var resultTestOutput = result.TestOutput.ToList();
        var regex = new Regex(@"Active downloads: (\d+)");
        foreach (var line in resultTestOutput)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                int activeDownloads = int.Parse(match.Groups[1].Value);
                Assert.True(activeDownloads <= int.Parse(maxParallelDownloads), $"Active downloads exceeded the limit: {activeDownloads} > {maxParallelDownloads}");
            }
        }
    }
}
