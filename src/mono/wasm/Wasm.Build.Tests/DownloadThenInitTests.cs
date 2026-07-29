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

        // Verify onConfigLoaded was called during download()
        Assert.Contains(resultTestOutput, s => s.Contains("onConfigLoaded was called during download"));

        // Verify resources were actually fetched during download
        var fetchesDuringDownload = resultTestOutput.Take(index + 1).Where(s => s.StartsWith("fetching")).ToList();
        Assert.True(fetchesDuringDownload.Count > 0, "Expected resources to be fetched during download()");

        // Verify no resources were re-fetched during create()
        var afterDownload = resultTestOutput.Skip(index + 1).Where(s => s.StartsWith("fetching")).ToList();
        if (afterDownload.Count > 0)
        {
            var duringDownload = resultTestOutput.Take(index + 1).Where(s => s.StartsWith("fetching")).ToList();
            var reFetchedResources = afterDownload.Intersect(duringDownload).ToList();
            if (reFetchedResources.Any())
                Assert.Fail($"Resources should not be fetched twice. Re-fetched on init: {string.Join(", ", reFetchedResources)}");
        }

        // Verify create() completed successfully
        Assert.Contains(resultTestOutput, s => s.Contains("create finished"));
    }

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task HttpCacheOnlyThenCreateWorks(Configuration config)
    {
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "DownloadThenInitHttpCacheOnly");
        BuildProject(info, config);
        BrowserRunOptions options = new(config, TestScenario: "DownloadThenInitHttpCacheOnly");
        RunResult result = await RunForBuildWithDotnetRun(options);
        var resultTestOutput = result.TestOutput.ToList();
        int index = resultTestOutput.FindIndex(s => s.Contains("download finished"));
        Assert.True(index > 0);

        // Verify loadBootResource was called during download(true)
        Assert.Contains(resultTestOutput, s => s.Contains("loadBootResource was called"));

        // Verify onConfigLoaded was called during download(true) — config init runs before prefetch
        Assert.Contains(resultTestOutput, s => s.Contains("onConfigLoaded was called during download"));

        // Verify prefetch requests happened during download
        var fetchesDuringDownload = resultTestOutput.Take(index + 1).Where(s => s.StartsWith("fetching")).ToList();
        Assert.True(fetchesDuringDownload.Count > 0, "Expected prefetch requests during download(true)");

        // Verify resource fetches happened during create() (httpCacheOnly doesn't load into memory)
        var fetchesAfterDownload = resultTestOutput.Skip(index + 1).Where(s => s.StartsWith("fetching")).ToList();
        Assert.True(fetchesAfterDownload.Count > 0, "Expected resource fetches during create() after httpCacheOnly download");

        // Verify create() completed successfully
        Assert.Contains(resultTestOutput, s => s.Contains("create finished"));
    }
}
