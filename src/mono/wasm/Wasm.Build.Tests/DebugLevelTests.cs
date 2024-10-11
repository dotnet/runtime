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

    private string SetupBrowserProject(string projectId, string extraProperties = "")
    {
        string id = $"{projectId}_{GetRandomId()}";
        string projectfile = CreateWasmTemplateProject(id, "wasmbrowser", extraProperties: extraProperties);

        UpdateBrowserMainJs();
        string mainJs = Path.Combine("wwwroot", "main.js");
        UpdateFile(mainJs, new Dictionary<string, string>
        {
            { "import { dotnet }", "import { dotnet, exit }" },
            {"await dotnet.run()", "console.log('TestOutput -> WasmDebugLevel: ' + config.debugLevel); exit(42)" }
        });
        return projectfile;
    }

    private void AssertDebugLevel(string result, int value)
        => Assert.Contains($"WasmDebugLevel: {value}", result);

    private BuildProjectOptions GetProjectOptions(bool isPublish = false) =>
        new BuildProjectOptions(
            DotnetWasmFromRuntimePack: !isPublish,
            CreateProject: false,
            HasV8Script: false,
            MainJS: "main.js",
            Publish: isPublish,
            AssertAppBundle: false
        );

    private string BuildPublishProject(string projectFile, string config, bool isPublish = false)
    {
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        var buildArgs = new BuildArgs(projectName, config, false, projectName, null);
        buildArgs = ExpandBuildArgs(buildArgs);
        (string _, string output) = BuildTemplateProject(buildArgs,
            buildArgs.Id,
            GetProjectOptions(isPublish)
        );
        return output;
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task BuildWithDefaultLevel(string configuration)
    {
        string projectFile = SetupBrowserProject($"DebugLevelTests_BuildWithDefaultLevel_{configuration}");
        BuildPublishProject(projectFile, configuration);

        string result = await RunBuiltBrowserApp(configuration, projectFile);
        AssertDebugLevel(result, -1);
    }

    [Theory]
    [InlineData("Debug", 1)]
    [InlineData("Release", 1)]
    [InlineData("Debug", 0)]
    [InlineData("Release", 0)]
    public async Task BuildWithExplicitValue(string configuration, int debugLevel)
    {
        string debugLvlProp = $"<WasmDebugLevel>{debugLevel}</WasmDebugLevel>";
        string projectFile = SetupBrowserProject($"DebugLevelTests_BuildWithExplicitValue_{configuration}", extraProperties: debugLvlProp);
        BuildPublishProject(projectFile, configuration);

        string result = await RunBuiltBrowserApp(configuration, projectFile);
        AssertDebugLevel(result, debugLevel);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task PublishWithDefaultLevel(string configuration)
    {
        string projectFile = SetupBrowserProject($"DebugLevelTests_PublishWithDefaultLevel_{configuration}");
        BuildPublishProject(projectFile, configuration, isPublish: true);

        string result = await RunPublishedBrowserApp(configuration);
        AssertDebugLevel(result, 0);
    }

    [Theory]
    [InlineData("Debug", 1)]
    [InlineData("Release", 1)]
    [InlineData("Debug", -1)]
    [InlineData("Release", -1)]
    public async Task PublishWithExplicitValue(string configuration, int debugLevel)
    {
        string debugLvlProp = $"<WasmDebugLevel>{debugLevel}</WasmDebugLevel>";
        string projectFile = SetupBrowserProject($"DebugLevelTests_PublishWithExplicitValue_{configuration}", debugLvlProp);
        BuildPublishProject(projectFile, configuration, isPublish: true);

        string result = await RunBuiltBrowserApp(configuration, projectFile);
        AssertDebugLevel(result, debugLevel);
    }
}
