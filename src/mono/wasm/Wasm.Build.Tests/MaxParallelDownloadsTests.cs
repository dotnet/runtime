// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;
using System.Text.RegularExpressions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests;

public class MaxParallelDownloadsTests : WasmTemplateTestsBase
{
    public MaxParallelDownloadsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData(Configuration.Release, "1")]
    [InlineData(Configuration.Release, "4")]
    public async Task NeverFetchMoreThanMaxAllowed(Configuration config, string maxParallelDownloads)
    {
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "MaxParallelDownloadsTests");
        BuildProject(info, config);
        RunResult result = await RunForBuildWithDotnetRun(new BrowserRunOptions(
            config,
            TestScenario: "MaxParallelDownloads",
            BrowserQueryString: new NameValueCollection { {"maxParallelDownloads", maxParallelDownloads } }
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
