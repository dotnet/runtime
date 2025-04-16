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

public class EnvVariablesTests : WasmTemplateTestsBase
{
    public EnvVariablesTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public async Task RunSimpleAppEnvVariables()
    {
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "EnvVariablesTest");

        BuildProject(info, config, new BuildOptions(AssertAppBundle: false), isNativeBuild: false);

        var result = await RunForBuildWithDotnetRun(new BrowserRunOptions(Configuration: config, TestScenario: "EnvVariablesTest"));
        Assert.Contains("foo=bar", result.TestOutput);
        Assert.Contains("baz=boo", result.TestOutput);
    }
}
