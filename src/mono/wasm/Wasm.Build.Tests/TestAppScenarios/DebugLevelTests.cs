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

public class WasmSdkDebugLevelTests : DebugLevelTests
{
    public WasmSdkDebugLevelTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    protected override void SetupProject(string projectId) => CopyTestAsset("WasmBasicTestApp", projectId, "App");
    
    protected override Task<RunResult> RunForBuild(string configuration) => RunSdkStyleAppForBuild(new(
        Configuration: configuration,
        TestScenario: "DebugLevelTest"
    ));

    protected override Task<RunResult> RunForPublish(string configuration) => RunSdkStyleAppForPublish(new(
        Configuration: configuration,
        TestScenario: "DebugLevelTest"
    ));
}

public class WasmAppBuilderDebugLevelTests : DebugLevelTests
{
    public WasmAppBuilderDebugLevelTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    protected override void SetupProject(string projectId)
    {
        Id = projectId;
        string projectfile = CreateWasmTemplateProject(projectId, "wasmconsole");
        string projectDir = Path.GetDirectoryName(projectfile)!;
        string mainJs = Path.Combine(projectDir, "main.mjs");
        string mainJsContent = File.ReadAllText(mainJs);
        mainJsContent = mainJsContent
            .Replace("import { dotnet }", "import { dotnet, exit }")
            .Replace("await runMainAndExit()", "testOutput('TestOutput -> ' + config.debugLevel); exit(0)");
        File.WriteAllText(mainJs, mainJsContent);
    }

    protected override Task<RunResult> RunForBuild(string configuration)
    {
        CommandResult res = new RunCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(_projectDir!)
            .ExecuteWithCapturedOutput($"run --no-silent --no-build -c {configuration}");

        return Task.FromResult(ProcessRunOutput(res));
    }

    private static RunResult ProcessRunOutput(CommandResult res)
    {
        var output = res.Output.Split(Environment.NewLine);
        var testOutput = output
            .Where(l => l.StartsWith("TestOutput -> "))
            .Select(l => l.Substring("TestOutput -> ".Length))
            .ToArray();

        return new RunResult(res.ExitCode, testOutput, output, []);
    }

    protected override Task<RunResult> RunForPublish(string configuration)
    {
        // TODO: Fix publish
        CommandResult res = new RunCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(_projectDir!)
            .ExecuteWithCapturedOutput($"run --no-silent --no-build -c {configuration}");

        return Task.FromResult(ProcessRunOutput(res));
    }
}

public abstract class DebugLevelTests : AppTestBase
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
        PublishProject(configuration);

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

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task PublishWithDefaultLevelAndPdbs(string configuration)
    {
        SetupProject($"DebugLevelTests_PublishWithDefaultLevelAndPdbs_{configuration}");
        PublishProject(configuration, assertAppBundle: false, extraArgs: $"-p:CopyOutputSymbolsToPublishDirectory=true");

        var result = await RunForPublish(configuration);
        AssertDebugLevel(result, -1);
    }
}
