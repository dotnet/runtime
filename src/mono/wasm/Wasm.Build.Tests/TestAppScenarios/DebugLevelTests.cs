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

public class DebugLevelTests : WasmTemplateTestsBase
{
    public DebugLevelTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    private void AssertDebugLevel(IReadOnlyCollection<string> result, int value)
        => Assert.Contains(result, m => m.Contains($"WasmDebugLevel: {value}"));

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task BuildWithDefaultLevel(string configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: BasicTestApp,
            idPrefix: "DebugLevelTests_BuildWithDefaultLevel"
        );
        BuildProject(info, config);
        RunOptions options = new(info.Configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForBuildWithDotnetRun(options);
        AssertDebugLevel(result.TestOutput, -1);
    }

    [Theory]
    [InlineData(Configuration.Debug, 1)]
    [InlineData(Configuration.Release, 1)]
    [InlineData(Configuration.Debug, 0)]
    [InlineData(Configuration.Release, 0)]
    public async Task BuildWithExplicitValue(string configuration, int debugLevel)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: BasicTestApp,
            idPrefix: "DebugLevelTests_BuildWithExplicitValue"
        );
        BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: $"-p:WasmDebugLevel={debugLevel}"));
        RunOptions options = new(info.Configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForBuildWithDotnetRun(options);
        AssertDebugLevel(result.TestOutput, debugLevel);
    }

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task PublishWithDefaultLevel(string configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: BasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithDefaultLevel"
        );
        PublishProject(info, config);
        RunOptions options = new(info.Configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, 0);
    }

    [Theory]
    [InlineData(Configuration.Debug, 1)]
    [InlineData(Configuration.Release, 1)]
    [InlineData(Configuration.Debug, -1)]
    [InlineData(Configuration.Release, -1)]
    public async Task PublishWithExplicitValue(string configuration, int debugLevel)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: BasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithExplicitValue"
        );
        PublishProject(info, isPublish: true, extraArgs: $"-p:WasmDebugLevel={debugLevel}");
        RunOptions options = new(info.Configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, debugLevel);
    }


    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task PublishWithDefaultLevelAndPdbs(string configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: BasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithDefaultLevelAndPdbs"
        );
        PublishProject(info, isPublish: true, extraArgs: $"-p:CopyOutputSymbolsToPublishDirectory=true");
        RunOptions options = new(info.Configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, -1);
    }
}
