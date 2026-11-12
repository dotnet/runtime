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

        var options = new BrowserRunOptions(Configuration: config, TestScenario: "EnvVariablesTest")
        {
            ExecuteAfterLoaded = async (_, page) =>
            {
                if (!IsCoreClrRuntime)
                    return;
                Assert.True(await page.EvaluateAsync<bool>("crossOriginIsolated"));
                var response = await page.APIRequest.GetAsync(page.Url);
                Assert.Equal("same-origin", response.Headers["cross-origin-opener-policy"]);
                Assert.Equal("require-corp", response.Headers["cross-origin-embedder-policy"]);
                await response.DisposeAsync();
            }
        };
        var result = await RunForBuildWithDotnetRun(options);
        Assert.Contains(result.TestOutput, m => m.Contains("foo=bar"));
        Assert.Contains(result.TestOutput, m => m.Contains("baz=boo"));
    }
}
