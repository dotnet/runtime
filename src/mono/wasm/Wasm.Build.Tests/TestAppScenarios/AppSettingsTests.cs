// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class AppSettingsTests : AppTestBase
{
    public AppSettingsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97054")]
    public async Task LoadAppSettingsBasedOnApplicationEnvironment(string applicationEnvironment)
    {
        CopyTestAsset("WasmBasicTestApp", "AppSettingsTests");
        PublishProject("Debug");

        var result = await RunSdkStyleApp(new(
            Configuration: "Debug",
            TestScenario: "AppSettingsTest",
            BrowserQueryString: new Dictionary<string, string> { ["applicationEnvironment"] = applicationEnvironment }
        ));
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal(GetFileExistenceMessage("/appsettings.json", true), m),
            m => Assert.Equal(GetFileExistenceMessage("/appsettings.Development.json", applicationEnvironment == "Development"), m),
            m => Assert.Equal(GetFileExistenceMessage("/appsettings.Production.json", applicationEnvironment == "Production"), m)
        );
    }

    // Synchronize with AppSettingsTest
    private static string GetFileExistenceMessage(string path, bool expected) => $"'{path}' exists '{expected}'";
}
