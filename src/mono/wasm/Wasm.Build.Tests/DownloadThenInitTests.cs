// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests;

public class DownloadThenInitTests : WasmTemplateTestsBase
{
    public DownloadThenInitTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task NoResourcesReFetchedAfterDownloadFinished(Configuration config)
    {
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "DownloadThenInitTests");
        BuildProject(info, config);
        BrowserRunOptions options = new(config, TestScenario: "DownloadThenInit");
        RunResult result = await RunForBuildWithDotnetRun(options);
        var resultTestOutput = result.TestOutput.ToList();
        int index = resultTestOutput.FindIndex(s => s.Contains("download finished"));
        Assert.True(index > 0); // number of fetched resources cannot be 0
        var afterDownload = resultTestOutput.Skip(index + 1).Where(s => s.StartsWith("fetching")).ToList();
        if (afterDownload.Count > 0)
        {
            var duringDownload = resultTestOutput.Take(index + 1).Where(s => s.StartsWith("fetching")).ToList();
            var reFetchedResources = afterDownload.Intersect(duringDownload).ToList();
            if (reFetchedResources.Any())
                Assert.Fail($"Resources should not be fetched twice. Re-fetched on init: {string.Join(", ", reFetchedResources)}");
        }
    }
}
