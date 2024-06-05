// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;
using Wasm.Build.Tests.TestAppScenarios;

#nullable enable

namespace Wasm.Build.Tests;

public class InitThenDownloadTests : AppTestBase
{
    public InitThenDownloadTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task ResourcesNotFetchedAfterDownloadFinished(string config)
    {
        CopyTestAsset("InitThenDownload");
        BuildProject(config);

        var result = await RunSdkStyleAppForBuild(new(Configuration: config));
        var resultTestOutput = result.TestOutput.ToList();
        int index = resultTestOutput.FindIndex(s => s == "download finished");
        Assert.True(index > 0); // number of fetched resources cannot be 0
        var fetchingStrings = resultTestOutput.Skip(index + 1).Where(s => s.StartsWith("fetching")).ToList();
        if (fetchingStrings.Count > 0)
        {
            // fetching only dotnet.native.wasm on init is acceptable
            Assert.True(fetchingStrings.Count == 1);
            Assert.EndsWith("dotnet.native.wasm", fetchingStrings[0]);
        }
    }
}
