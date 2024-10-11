// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Wasm.Build.Tests.TestAppScenarios;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class DebugLevelTests : AppTestBase
{
    public DebugLevelTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task PublishWithDefaultLevelAndPdbs(string configuration)
    {
        CopyTestAsset("WasmBasicTestApp", $"DebugLevelTests_PublishWithDefaultLevelAndPdbs_{configuration}", "App");
        PublishProject(configuration, assertAppBundle: false, extraArgs: $"-p:CopyOutputSymbolsToPublishDirectory=true");

        var result = await RunSdkStyleAppForBuild(new(
            Configuration: configuration,
            TestScenario: "DebugLevelTest"
        ));
        Assert.Contains($"WasmDebugLevel: -1", result.TestOutput);
    }
}
