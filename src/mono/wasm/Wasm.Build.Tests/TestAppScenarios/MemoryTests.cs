// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class MemoryTests : AppTestBase
{
    public MemoryTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Release", true)]
    [InlineData("Release", false)]
    public async Task AllocateLargeHeapThenRepeatedlyInterop(string config, bool buildNative)
    {
        CopyTestAsset("WasmBasicTestApp", "MemoryTests", "App");
        string extraArgs = $"-p:EmccMaximumHeapSize=4294901760";
        if (buildNative)
            extraArgs += $" -p:WasmBuildNative=true";
        BuildProject(config, assertAppBundle: false, extraArgs: extraArgs);

        int expectedCode = BuildTestBase.IsUsingWorkloads ? 0 : 1;
        var result = await RunSdkStyleAppForBuild(new (Configuration: config, TestScenario: "AllocateLargeHeapThenInterop", ExpectedExitCode: expectedCode));
        if(!BuildTestBase.IsUsingWorkloads)
        {
            Assert.Contains(result.TestOutput, item => item.Contains("To build this project, the following workloads must be installed: wasm-tools"));
        }
    }
}
