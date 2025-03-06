// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class AppSettingsTests : WasmTemplateTestsBase
{
    public AppSettingsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Development", null)]
    [InlineData("Production", null)]
    [InlineData(null, "Development")]
    [InlineData(null, "Production")]
    [InlineData("Production", "Development")]
    [InlineData("Development", "Production")]
    public async Task LoadAppSettingsBasedOnApplicationEnvironment(string msBuildApplicationEnvironment, string queryApplicationEnvironment)
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "AppSettingsTest");
        PublishProject(
            info, 
            config,
            new PublishOptions(ExtraMSBuildArgs: $"-p:WasmApplicationEnvironmentName={msBuildApplicationEnvironment}")
        );
        BrowserRunOptions options = new(
            config,
            TestScenario: "AppSettingsTest",
            BrowserQueryString: new NameValueCollection { { "applicationEnvironment", queryApplicationEnvironment } }
        );
        RunResult result = await RunForPublishWithWebServer(options);

        string effectiveApplicationEnvironment = queryApplicationEnvironment ?? msBuildApplicationEnvironment;
        Assert.Contains(result.TestOutput, m => m.Contains("'/appsettings.json' exists 'True'"));
        Assert.Contains(result.TestOutput, m => m.Contains($"'/appsettings.Development.json' exists '{effectiveApplicationEnvironment == "Development"}'"));
        Assert.Contains(result.TestOutput, m => m.Contains($"'/appsettings.Production.json' exists '{effectiveApplicationEnvironment == "Production"}'"));
    }
}
