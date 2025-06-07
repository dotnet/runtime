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

public class MiscTests : WasmTemplateTestsBase
{
    public MiscTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public async Task TestGetFunctionPointerForDelegate()
    {
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "GetFunctionPointerForDelegate");

        BuildProject(info, config, new BuildOptions(AssertAppBundle: false), isNativeBuild: false);

        var result = await RunForBuildWithDotnetRun(new BrowserRunOptions(Configuration: config, TestScenario: "GetFunctionPointerForDelegate"));

        Assert.Equal(2, result.TestOutput.Count);
        Assert.Contains(result.TestOutput, m => m.Contains("System.PlatformNotSupportedException"));
        Assert.Contains(result.TestOutput, m => m.Contains("Dynamic entrypoint allocation is not supported in the current environment."));
    }
}
