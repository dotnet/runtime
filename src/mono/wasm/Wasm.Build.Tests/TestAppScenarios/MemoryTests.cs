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

    // ActiveIssue: https://github.com/dotnet/runtime/issues/104618
    [Fact, TestCategory("no-workload")]
    public async Task AllocateLargeHeapThenRepeatedlyInterop_NoWorkload() =>
        await AllocateLargeHeapThenRepeatedlyInterop();

    [Fact]
    public async Task AllocateLargeHeapThenRepeatedlyInterop()
    {
        string config = "Release";
        CopyTestAsset("WasmBasicTestApp", "MemoryTests", "App");
        string extraArgs = BuildTestBase.IsUsingWorkloads ? "-p:EmccMaximumHeapSize=4294901760" : "-p:EmccMaximumHeapSize=4294901760 -p:LazyLoad=false";
        BuildProject(config, assertAppBundle: false, extraArgs: extraArgs, expectSuccess: BuildTestBase.IsUsingWorkloads);

        if (BuildTestBase.IsUsingWorkloads)
        {
            await RunSdkStyleAppForBuild(new (Configuration: config, TestScenario: "AllocateLargeHeapThenInterop"));
        }
    }
}
