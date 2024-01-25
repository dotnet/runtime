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

public class DebugLevelTests : AppTestBase
{
    public DebugLevelTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    private void AssertDebugLevel(RunResult result, int value) 
    {
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal($"WasmDebugLevel: {value}", m)
        );
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task BuildWithDefaultLevel(string configuration)
    {
        CopyTestAsset("WasmBasicTestApp", "DebugLevelTests_BuildWithDefaultLevel");
        BuildProject(configuration);

        var result = await RunSdkStyleAppForBuild(new(
            Configuration: configuration,
            TestScenario: "DebugLevelTest"
        ));
        AssertDebugLevel(result, -1);
    }

    [Theory]
    [InlineData("Debug", 1)]
    [InlineData("Release", 1)]
    [InlineData("Debug", 0)]
    [InlineData("Release", 0)]
    public async Task BuildWithExplicitValue(string configuration, int debugLevel)
    {
        CopyTestAsset("WasmBasicTestApp", "DebugLevelTests_BuildWithExplicitValue");
        BuildProject(configuration, $"-p:WasmDebugLevel={debugLevel}");

        var result = await RunSdkStyleAppForBuild(new(
            Configuration: configuration,
            TestScenario: "DebugLevelTest"
        ));
        AssertDebugLevel(result, debugLevel);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task PublishWithDefaultLevel(string configuration)
    {
        CopyTestAsset("WasmBasicTestApp", "DebugLevelTests_PublishWithDefaultLevel");
        PublishProject(configuration);

        var result = await RunSdkStyleAppForPublish(new(
            Configuration: configuration,
            TestScenario: "DebugLevelTest"
        ));
        AssertDebugLevel(result, 0);
    }

    [Theory]
    [InlineData("Debug", 1)]
    [InlineData("Release", 1)]
    [InlineData("Debug", -1)]
    [InlineData("Release", -1)]
    public async Task PublishWithExplicitValue(string configuration, int debugLevel)
    {
        CopyTestAsset("WasmBasicTestApp", "DebugLevelTests_PublishWithExplicitValue");
        PublishProject(configuration, $"-p:WasmDebugLevel={debugLevel}");

        var result = await RunSdkStyleAppForPublish(new(
            Configuration: configuration,
            TestScenario: "DebugLevelTest"
        ));
        AssertDebugLevel(result, debugLevel);
    }
}