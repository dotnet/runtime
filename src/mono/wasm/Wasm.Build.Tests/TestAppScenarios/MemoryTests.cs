// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class MemoryTests : WasmTemplateTestsBase
{
    public MemoryTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    // ActiveIssue: https://github.com/dotnet/runtime/issues/104618
    [Fact, TestCategory("no-workload")]
    public async Task AllocateLargeHeapThenRepeatedlyInterop_NoWorkload() =>
        await AllocateLargeHeapThenRepeatedlyInterop();

    [Fact]
    public async Task AllocateLargeHeapThenRepeatedlyInterop()
    {
        string config = "Release";
        ProjectInfo info = CopyTestAsset(config, false, "WasmBasicTestApp", "MemoryTests", "App");
        bool isPublish = false;
        string extraArgs = "-p:EmccMaximumHeapSize=4294901760";
        BuildTemplateProject(info,
            new BuildProjectOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                // using EmccMaximumHeapSize forces native rebuild
                ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish, isNativeBuild: true),
                IsPublish: isPublish,
                ExpectSuccess: BuildTestBase.IsUsingWorkloads
            ),
            extraArgs: extraArgs
        );

        if (BuildTestBase.IsUsingWorkloads)
        {
            await RunForBuildWithDotnetRun(new (Configuration: config, TestScenario: "AllocateLargeHeapThenInterop"));
        }
    }

    [Fact]
    public async Task RunSimpleAppWithProfiler()
    {
        string config = "Release";
        ProjectInfo info = CopyTestAsset(config, false, "WasmBasicTestApp", "ProfilerTest", "App");
        bool isPublish = false;
        // are are linking all 3 profilers, but below we only initialize log profiler and test it
        string extraArgs = $"-p:WasmProfilers=\"aot+browser+log\" -p:WasmBuildNative=true";
        BuildTemplateProject(info,
            new BuildProjectOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish),
                IsPublish: isPublish,
                AssertAppBundle: false
            ),
            extraArgs: extraArgs
        );

        var result = await RunForBuildWithDotnetRun(new (Configuration: config, TestScenario: "ProfilerTest"));
        Regex regex = new Regex(@"Profile data of size (\d+) bytes");
        var match = result.TestOutput
            .Select(line => regex.Match(line))
            .FirstOrDefault(m => m.Success);
        Assert.True(match != null, $"TestOuptup did not contain log matching {regex}");
        if (!int.TryParse(match.Groups[1].Value, out int fileSize))
        {
            Assert.Fail($"Failed to parse profile size from {match.Groups[1].Value} to int");
        }
        Assert.True(fileSize >= 10 * 1024, $"Profile file size is less than 10KB. Actual size: {fileSize} bytes.");
    }
}
