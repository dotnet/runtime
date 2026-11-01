// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests;

[TestCategory("mono")]
public class DiagnosticsTests : WasmTemplateTestsBase
{
    public DiagnosticsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public async Task RunSimpleAppWithBrowserProfiler()
    {
        Configuration config = Configuration.Release;

        string extraProperties = "<WasmProfilers>browser:callspec=all,interval=0</WasmProfilers><WasmDebugLevel>0</WasmDebugLevel>";
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "BrowserProfilerTest", extraProperties: extraProperties);

        BuildProject(info, config, new BuildOptions(AssertAppBundle: false, EnableDiagnostics: true), isNativeBuild: true);

        await RunForBuildWithDotnetRun(new BrowserRunOptions(Configuration: config, TestScenario: "BrowserProfilerTest"));
    }
}
