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

namespace Wasm.Build.Tests;

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
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "MemoryTests");
        string extraArgs = "-p:EmccMaximumHeapSize=4294901760";
        BuildProject(info,
            config,
            new BuildOptions(ExtraMSBuildArgs: extraArgs, ExpectSuccess: BuildTestBase.IsUsingWorkloads),
            // using EmccMaximumHeapSize forces native rebuild
            isNativeBuild: true);

        if (BuildTestBase.IsUsingWorkloads)
        {
            await RunForBuildWithDotnetRun(new BrowserRunOptions(
                Configuration: config,
                TestScenario: "AllocateLargeHeapThenInterop"
            ));
        }
    }

    [Fact]
    public async Task RunSimpleAppWithProfiler()
    {
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "ProfilerTest");
        // are are linking all 3 profilers, but below we only initialize log profiler and test it
        string extraArgs = $"-p:WasmProfilers=\"aot+browser+log\" -p:WasmBuildNative=true";
        BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: extraArgs, AssertAppBundle: false), isNativeBuild: true);

        var result = await RunForBuildWithDotnetRun(new BrowserRunOptions(Configuration: config, TestScenario: "ProfilerTest"));
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
