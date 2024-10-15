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

namespace Wasm.Build.Tests;

// ToDo: fix to be based on WasmTemplateTestBase
public class DebugLevelTests : AppTestBase
{
    public DebugLevelTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    private void AssertDebugLevel(string result, int value)
        => Assert.Contains($"WasmDebugLevel: {value}", result);

    private BuildProjectOptions GetProjectOptions(bool isPublish = false) =>
        new BuildProjectOptions(
            DotnetWasmFromRuntimePack: !isPublish,
            CreateProject: false,
            MainJS: "main.js",
            HasV8Script: false,
            Publish: isPublish,
            AssertAppBundle: false,
            UseCache: false
        );

    private string BuildPublishProject(string projectName, string config, bool isPublish = false, params string[] extraArgs)
    {
        var buildArgs = new BuildArgs(projectName, config, false, projectName, null);
        buildArgs = ExpandBuildArgs(buildArgs);
        (string _, string output) = BuildTemplateProject(buildArgs,
            buildArgs.Id,
            GetProjectOptions(isPublish),
            extraArgs: extraArgs
        );
        return output;
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task BuildWithDefaultLevel(string configuration)
    {
        string testAssetName = "WasmBasicTestApp";
        string projectFile = $"{testAssetName}.csproj";
        CopyTestAsset(testAssetName, $"DebugLevelTests_BuildWithDefaultLevel_{configuration}", "App");
        BuildPublishProject(projectFile, configuration);

        string result = await RunBuiltBrowserApp(configuration, projectFile, testScenario: "DebugLevelTest");
        AssertDebugLevel(result, -1);
    }

    [Theory]
    [InlineData("Debug", 1)]
    [InlineData("Release", 1)]
    [InlineData("Debug", 0)]
    [InlineData("Release", 0)]
    public async Task BuildWithExplicitValue(string configuration, int debugLevel)
    {
        string testAssetName = "WasmBasicTestApp";
        string projectFile = $"{testAssetName}.csproj";
        CopyTestAsset(testAssetName, $"DebugLevelTests_BuildWithExplicitValue_{configuration}", "App");
        BuildPublishProject(projectFile, configuration, extraArgs: $"-p:WasmDebugLevel={debugLevel}");

        string result = await RunBuiltBrowserApp(configuration, projectFile, testScenario: "DebugLevelTest");
        AssertDebugLevel(result, debugLevel);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task PublishWithDefaultLevel(string configuration)
    {
        string testAssetName = "WasmBasicTestApp";
        string projectFile = $"{testAssetName}.csproj";
        CopyTestAsset(testAssetName, $"DebugLevelTests_PublishWithDefaultLevel_{configuration}", "App");
        BuildPublishProject(projectFile, configuration, isPublish: true);

        string result = await RunPublishedBrowserApp(configuration, testScenario: "DebugLevelTest");
        AssertDebugLevel(result, 0);
    }

    [Theory]
    [InlineData("Debug", 1)]
    [InlineData("Release", 1)]
    [InlineData("Debug", -1)]
    [InlineData("Release", -1)]
    public async Task PublishWithExplicitValue(string configuration, int debugLevel)
    {
        string testAssetName = "WasmBasicTestApp";
        string projectFile = $"{testAssetName}.csproj";
        CopyTestAsset(testAssetName, $"DebugLevelTests_PublishWithExplicitValue_{configuration}", "App");
        BuildPublishProject(projectFile, configuration, isPublish: true, extraArgs: $"-p:WasmDebugLevel={debugLevel}");

        string result = await RunBuiltBrowserApp(configuration, projectFile, testScenario: "DebugLevelTest");
        AssertDebugLevel(result, debugLevel);
    }
    

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task PublishWithDefaultLevelAndPdbs(string configuration)
    {
        string testAssetName = "WasmBasicTestApp";
        string projectFile = $"{testAssetName}.csproj";
        CopyTestAsset(testAssetName, $"DebugLevelTests_PublishWithDefaultLevelAndPdbs_{configuration}", "App");
        BuildPublishProject(projectFile, configuration, isPublish: true, extraArgs: $"-p:CopyOutputSymbolsToPublishDirectory=true");

        var result = await RunPublishedBrowserApp(configuration, testScenario: "DebugLevelTest");
        AssertDebugLevel(result, -1);
    }
}
