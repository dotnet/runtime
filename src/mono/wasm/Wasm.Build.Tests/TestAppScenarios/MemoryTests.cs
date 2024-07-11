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
    [InlineData("Release", true, false)]
    [InlineData("Release", false, false)]
    [InlineData("Release", false, true)]
    public async Task AllocateLargeHeapThenRepeatedlyInterop(string config, bool buildNative, bool useWorkaround)
    {
        // native build triggers passing value form EmccMaximumHeapSize to MAXIMUM_MEMORY that is set in emscripten
        // in non-native build EmccMaximumHeapSize does not have an effect, so the test will fail with "out of memory"
        // a workaround is to set <EmccFlags> manually
        CopyTestAsset("WasmBasicTestApp", "MemoryTests", "App");
        string extraArgs = $"-p:EmccMaximumHeapSize=4294901760 -p:WasmBuildNative={buildNative}";
        if (useWorkaround)
            extraArgs += $"-p:EmccFlags=4294901760";
        BuildProject(config, assertAppBundle: false, extraArgs: $"-p:EmccMaximumHeapSize=4294901760 -p:WasmBuildNative={buildNative}");

        var result = await RunSdkStyleAppForBuild(new (Configuration: config, TestScenario: "AllocateLargeHeapThenInterop", ExpectedExitCode: buildNative ? 0 : 1));
        if (!buildNative && !useWorkaround)
            Assert.Contains(result.TestOutput, item => item.Contains("Exception System.OutOfMemoryException: Out of memory"));
    }
}
