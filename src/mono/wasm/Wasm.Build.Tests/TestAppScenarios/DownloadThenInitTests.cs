// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class DownloadThenInitTests : AppTestBase
{
    public DownloadThenInitTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task NoResourcesReFetchedAfterDownloadFinished(string config)
    {
        CopyTestAsset("WasmBasicTestApp", "DownloadThenInitTests", "App");
        BuildProject(config);

        var result = await RunSdkStyleAppForBuild(new(Configuration: config, TestScenario: "DownloadThenInit"));
        var resultTestOutput = result.TestOutput.ToList();
        int index = resultTestOutput.FindIndex(s => s == "download finished");
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
