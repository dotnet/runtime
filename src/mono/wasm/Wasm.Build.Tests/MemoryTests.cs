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

[TestCategory("native")]
public class MemoryTests : WasmTemplateTestsBase
{
    public MemoryTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact, TestCategory("no-workload")]
    public async Task AllocateLargeHeapThenRepeatedlyInterop_NoWorkload() =>
        await AllocateLargeHeapThenRepeatedlyInterop();

    [Fact, TestCategory("mono")] // TODO-WASM https://github.com/dotnet/runtime/issues/132555
    public async Task AllocateLargeHeapThenRepeatedlyInterop()
    {
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "MemoryTests");
        string extraArgs = "-p:EmccMaximumHeapSize=4294901760";
        // TODO-WASM https://github.com/dotnet/runtime/issues/126100 Pass default property values from runtime (pack) build to the relink.
        if (BuildTestBase.IsCoreClrRuntime)
            extraArgs += " -p:WasmBuildNative=true";

        BuildProject(info,
            config,
            new BuildOptions(ExtraMSBuildArgs: extraArgs, ExpectSuccess: BuildTestBase.IsUsingWorkloads),
            // using EmccMaximumHeapSize forces native rebuild
            isNativeBuild: true);

        if (BuildTestBase.IsUsingWorkloads)
        {
            RunResult result = await RunForBuildWithDotnetRun(new BrowserRunOptions(
                Configuration: config,
                TestScenario: "AllocateLargeHeapThenInterop"
            ));

            Assert.Contains(result.TestOutput, line => line.Contains("Great success, MemoryTest finished without errors."));
            // above the 2GB boundary the jiterpreter used to encode negative pointers and emit invalid wasm modules
            Assert.DoesNotContain(result.ConsoleOutput, line => line.Contains("code generation failed"));
        }
    }
}
