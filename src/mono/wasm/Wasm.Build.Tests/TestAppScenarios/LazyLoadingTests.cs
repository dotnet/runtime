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

public class LazyLoadingTests : AppTestBase
{
    public LazyLoadingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97054")]
    public async Task LoadLazyAssemblyBeforeItIsNeeded()
    {
        CopyTestAsset("WasmBasicTestApp", "LazyLoadingTests");
        PublishProject("Debug");

        var result = await RunSdkStyleApp(new(Configuration: "Debug", TestScenario: "LazyLoadingTest"));
        Assert.True(result.TestOutput.Any(m => m.Contains("FirstName")), "The lazy loading test didn't emit expected message with JSON");
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97054")]
    public async Task FailOnMissingLazyAssembly()
    {
        CopyTestAsset("WasmBasicTestApp", "LazyLoadingTests");
        PublishProject("Debug");

        var result = await RunSdkStyleApp(new(
            Configuration: "Debug",
            TestScenario: "LazyLoadingTest",
            BrowserQueryString: new Dictionary<string, string> { ["loadRequiredAssembly"] = "false" },
            ExpectedExitCode: 1
        ));
        Assert.True(result.ConsoleOutput.Any(m => m.Contains("Could not load file or assembly") && m.Contains("Json")), "The lazy loading test didn't emit expected error message");
    }
}
