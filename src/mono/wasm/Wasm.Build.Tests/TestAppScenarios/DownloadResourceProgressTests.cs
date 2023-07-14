// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class DownloadResourceProgressTests : AppTestBase
{
    public DownloadResourceProgressTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DownloadProgressFinishes(bool fetchFailure)
    {
        CopyTestAsset("WasmBasicTestApp", "DownloadResourceProgressTests");
        PublishProject("Debug");

        var result = await RunSdkStyleApp(new(
            Configuration: "Debug",
            ForPublish: true,
            TestScenario: "DownloadResourceProgressTest",
            BrowserQueryString: new Dictionary<string, string> { ["fetchFailure"] = fetchFailure.ToString().ToLowerInvariant() }
        ));
        Assert.True(result.TestOutput.Any(m => m.Contains("DownloadResourceProgress: Finished")), "The download progress test didn't emit expected error message");
    }
}
