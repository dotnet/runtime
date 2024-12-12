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
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task LoadAppSettingsBasedOnApplicationEnvironment(string applicationEnvironment)
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "AppSettingsTest");
        PublishProject(info, config);
        BrowserRunOptions options = new(
            config,
            TestScenario: "AppSettingsTest",
            BrowserQueryString: new NameValueCollection { { "applicationEnvironment", applicationEnvironment } }
        );
        RunResult result = await RunForPublishWithWebServer(options);
        Assert.Contains(result.TestOutput, m => m.Contains("'/appsettings.json' exists 'True'"));
        Assert.Contains(result.TestOutput, m => m.Contains($"'/appsettings.Development.json' exists '{applicationEnvironment == "Development"}'"));
        Assert.Contains(result.TestOutput, m => m.Contains($"'/appsettings.Production.json' exists '{applicationEnvironment == "Production"}'"));
    }
}
