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

    public static IEnumerable<object?[]> LoadAppSettingsBasedOnApplicationEnvironmentData()
    {
        // Defaults
        yield return new object?[] { false, null, null, "Development" };
        yield return new object?[] { true, null, null, "Production" };

        // Override defaults from MSBuild
        yield return new object?[] { false, "Production", null, "Production" };
        yield return new object?[] { true, "Development", null, "Development" };

        // Override defaults from JavaScript
        yield return new object?[] { false, null, "Production", "Production" };
        yield return new object?[] { true, null, "Development", "Development" };

        // Override MSBuild from JavaScript
        yield return new object?[] { false, "FromMSBuild", "Production", "Production" };
        yield return new object?[] { true, "FromMSBuild", "Development", "Development" };
    }

    [Theory]
    [MemberData(nameof(LoadAppSettingsBasedOnApplicationEnvironmentData))]
    public async Task LoadAppSettingsBasedOnApplicationEnvironment(bool publish, string? msBuildApplicationEnvironment, string? queryApplicationEnvironment, string expectedApplicationEnvironment)
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "AppSettingsTest");
        string extraMSBuildArgs = msBuildApplicationEnvironment != null ? $"-p:WasmApplicationEnvironmentName={msBuildApplicationEnvironment}" : string.Empty;

        if (publish)
            PublishProject(info, config, new PublishOptions(ExtraMSBuildArgs: extraMSBuildArgs));
        else
            BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: extraMSBuildArgs));
        
        BrowserRunOptions runOptions = new(
            config,
            TestScenario: "AppSettingsTest",
            BrowserQueryString: new NameValueCollection { { "applicationEnvironment", queryApplicationEnvironment } }
        );
        RunResult result = publish
            ? await RunForPublishWithWebServer(runOptions)
            : await RunForBuildWithDotnetRun(runOptions);

        Assert.Contains(result.TestOutput, m => m.Contains("'/appsettings.json' exists 'True'"));
        Assert.Contains(result.TestOutput, m => m.Contains($"'/appsettings.Development.json' exists '{expectedApplicationEnvironment == "Development"}'"));
        Assert.Contains(result.TestOutput, m => m.Contains($"'/appsettings.Production.json' exists '{expectedApplicationEnvironment == "Production"}'"));
    }
}
