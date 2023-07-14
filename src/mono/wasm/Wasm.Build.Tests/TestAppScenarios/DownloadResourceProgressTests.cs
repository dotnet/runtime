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
    public AppSettingsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public async Task DownloadProgressFinishes()
    {
        CopyTestAsset("WasmBasicTestApp", "DownloadResourceProgressTests");
        PublishProject("Debug");

        var result = await RunSdkStyleApp(new(
            Configuration: "Debug",
            ForPublish: true,
            TestScenario: "DownloadResourceProgressTest"
        ));
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal("DownloadResourceProgress: Finished", m),
        );
    }
}
