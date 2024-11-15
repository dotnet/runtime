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

    private void BuildPublishProject(ProjectInfo info, bool isPublish = false, params string[] extraArgs)
       => BuildProject(info,
            new BuildOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish),
                IsPublish: isPublish
            ),
            extraArgs: extraArgs
        );

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task BuildWithDefaultLevel(string configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: BasicTestApp,
            idPrefix: "DebugLevelTests_BuildWithDefaultLevel"
        );
        BuildPublishProject(info);
        RunOptions options = new(info.Configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForBuildWithDotnetRun(options);
        AssertDebugLevel(result.TestOutput, -1);
    }

    [Theory]
    [InlineData("Debug", 1)]
    [InlineData("Release", 1)]
    [InlineData("Debug", 0)]
    [InlineData("Release", 0)]
    public async Task BuildWithExplicitValue(string configuration, int debugLevel)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: BasicTestApp,
            idPrefix: "DebugLevelTests_BuildWithExplicitValue"
        );
        BuildPublishProject(info, extraArgs: $"-p:WasmDebugLevel={debugLevel}");
        RunOptions options = new(info.Configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForBuildWithDotnetRun(options);
        AssertDebugLevel(result.TestOutput, debugLevel);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task PublishWithDefaultLevel(string configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: BasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithDefaultLevel"
        );
        BuildPublishProject(info, isPublish: true);
        RunOptions options = new(info.Configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, 0);
    }

    [Theory]
    [InlineData("Debug", 1)]
    [InlineData("Release", 1)]
    [InlineData("Debug", -1)]
    [InlineData("Release", -1)]
    public async Task PublishWithExplicitValue(string configuration, int debugLevel)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: BasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithExplicitValue"
        );
        BuildPublishProject(info, isPublish: true, extraArgs: $"-p:WasmDebugLevel={debugLevel}");
        RunOptions options = new(info.Configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, debugLevel);
    }


    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task PublishWithDefaultLevelAndPdbs(string configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: BasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithDefaultLevelAndPdbs"
        );
        BuildPublishProject(info, isPublish: true, extraArgs: $"-p:CopyOutputSymbolsToPublishDirectory=true");
        RunOptions options = new(info.Configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, -1);
    }
}
