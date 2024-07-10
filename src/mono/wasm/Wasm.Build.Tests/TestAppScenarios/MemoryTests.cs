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
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task AllocateLargeHeapThenRepeatedlyInterop(string config)
    {
        CopyTestAsset("WasmBasicTestApp", "MemoryTests", "App");
        BuildProject(config, extraArgs: $"-p:EmccMaximumHeapSize=4294901760");

        var result = await RunSdkStyleAppForBuild(new (Configuration: config, TestScenario: "AllocateLargeHeapThenInterop"));
        Console.WriteLine(result.TestOutput);
    }
}
