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

namespace Wasm.Build.Tests;

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
    public async Task BuildWithDefaultLevel(Configuration configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: TestAsset.WasmBasicTestApp,
            idPrefix: "DebugLevelTests_BuildWithDefaultLevel"
        );
        BuildProject(info, configuration);
        BrowserRunOptions options = new(configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForBuildWithDotnetRun(options);
        AssertDebugLevel(result.TestOutput, -1);
    }

    [Theory]
    [InlineData(Configuration.Debug, 1)]
    [InlineData(Configuration.Release, 1)]
    [InlineData(Configuration.Debug, 0)]
    [InlineData(Configuration.Release, 0)]
    public async Task BuildWithExplicitValue(Configuration configuration, int debugLevel)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: TestAsset.WasmBasicTestApp,
            idPrefix: "DebugLevelTests_BuildWithExplicitValue"
        );
        BuildProject(info, configuration, new BuildOptions(ExtraMSBuildArgs: $"-p:WasmDebugLevel={debugLevel}"));
        BrowserRunOptions options = new(configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForBuildWithDotnetRun(options);
        AssertDebugLevel(result.TestOutput, debugLevel);
    }

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task PublishWithDefaultLevel(Configuration configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: TestAsset.WasmBasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithDefaultLevel"
        );
        PublishProject(info, configuration);
        BrowserRunOptions options = new(configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, 0);
    }

    [Theory]
    [InlineData(Configuration.Debug, 1)]
    [InlineData(Configuration.Release, 1)]
    [InlineData(Configuration.Debug, -1)]
    [InlineData(Configuration.Release, -1)]
    public async Task PublishWithExplicitValue(Configuration configuration, int debugLevel)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: TestAsset.WasmBasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithExplicitValue"
        );
        PublishProject(info, configuration, new PublishOptions(ExtraMSBuildArgs: $"-p:WasmDebugLevel={debugLevel}"));
        BrowserRunOptions options = new(configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, debugLevel);
    }


    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task PublishWithDefaultLevelAndPdbs(Configuration configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: TestAsset.WasmBasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithDefaultLevelAndPdbs"
        );
        PublishProject(info, configuration, new PublishOptions(ExtraMSBuildArgs: $"-p:CopyOutputSymbolsToPublishDirectory=true"));
        BrowserRunOptions options = new(configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, -1);
    }
}
