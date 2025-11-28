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

public class HttpTests : WasmTemplateTestsBase
{
    public HttpTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    // testing that WasmEnableStreamingResponse=false MSbuild prop is passed to the app and HTTP behaves as expected
    public async Task HttpNoStreamingTest()
    {
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "HttpTest");

        BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: "-p:WasmEnableStreamingResponse=false",AssertAppBundle: false), isNativeBuild: false);

        var result = await RunForBuildWithDotnetRun(new BrowserRunOptions(Configuration: config, TestScenario: "HttpNoStreamingTest"));

        Assert.Contains(result.TestOutput, m => m.Contains("AppContext FeatureEnableStreamingResponse=False"));
        Assert.Contains(result.TestOutput, m => m.Contains("response.Content is System.Net.Http.BrowserHttpContent"));
    }
}
