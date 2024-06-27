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

public abstract class DebugLevelTestsBase : AppTestBase
{
    public DebugLevelTestsBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    protected void AssertDebugLevel(RunResult result, int value)
    {
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal($"WasmDebugLevel: {value}", m)
        );
    }

    protected abstract void SetupProject(string projectId);
    protected abstract Task<RunResult> RunForBuild(string configuration);
    protected abstract Task<RunResult> RunForPublish(string configuration);

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task BuildWithDefaultLevel(string configuration)
    {
        SetupProject($"DebugLevelTests_BuildWithDefaultLevel_{configuration}");
        BuildProject(configuration, assertAppBundle: false);

        var result = await RunForBuild(configuration);
        AssertDebugLevel(result, -1);
    }

    [Theory]
    [InlineData("Debug", 1)]
    [InlineData("Release", 1)]
    [InlineData("Debug", 0)]
    [InlineData("Release", 0)]
    public async Task BuildWithExplicitValue(string configuration, int debugLevel)
    {
        SetupProject($"DebugLevelTests_BuildWithExplicitValue_{configuration}");
        BuildProject(configuration, assertAppBundle: false, extraArgs: $"-p:WasmDebugLevel={debugLevel}");

        var result = await RunForBuild(configuration);
        AssertDebugLevel(result, debugLevel);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task PublishWithDefaultLevel(string configuration)
    {
        SetupProject($"DebugLevelTests_PublishWithDefaultLevel_{configuration}");
        PublishProject(configuration, assertAppBundle: false);

        var result = await RunForPublish(configuration);
        AssertDebugLevel(result, 0);
    }

    [Theory]
    [InlineData("Debug", 1)]
    [InlineData("Release", 1)]
    [InlineData("Debug", -1)]
    [InlineData("Release", -1)]
    public async Task PublishWithExplicitValue(string configuration, int debugLevel)
    {
        SetupProject($"DebugLevelTests_PublishWithExplicitValue_{configuration}");
        PublishProject(configuration, assertAppBundle: false, extraArgs: $"-p:WasmDebugLevel={debugLevel}");

        var result = await RunForPublish(configuration);
        AssertDebugLevel(result, debugLevel);
    }
}
