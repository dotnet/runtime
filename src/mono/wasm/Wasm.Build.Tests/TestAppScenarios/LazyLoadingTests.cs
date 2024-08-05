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

    public static IEnumerable<object?[]> LoadLazyAssemblyBeforeItIsNeededData()
    {
        string[] data = ["wasm", "dll", "NoExtension"];
        return data.Select(d => new object[] { d, data });
    }

    [Theory, TestCategory("no-fingerprinting")]
    [MemberData(nameof(LoadLazyAssemblyBeforeItIsNeededData))]
    public async Task LoadLazyAssemblyBeforeItIsNeeded(string lazyLoadingTestExtension, string[] allLazyLoadingTestExtensions)
    {
        CopyTestAsset("WasmBasicTestApp", "LazyLoadingTests", "App");
        BuildProject("Debug", extraArgs: $"-p:LazyLoadingTestExtension={lazyLoadingTestExtension}");

        // We are running the app and passing all possible lazy extensions to test matrix of all possibilities.
        // We don't need to rebuild the application to test how client is trying to load the assembly.
        foreach (var clientLazyLoadingTestExtension in allLazyLoadingTestExtensions)
        {
            var result = await RunSdkStyleAppForBuild(new(
                Configuration: "Debug", 
                TestScenario: "LazyLoadingTest", 
                BrowserQueryString: new Dictionary<string, string> { ["lazyLoadingTestExtension"] = clientLazyLoadingTestExtension }
            ));

            Assert.True(result.TestOutput.Any(m => m.Contains("FirstName")), "The lazy loading test didn't emit expected message with JSON");
            Assert.True(result.ConsoleOutput.Any(m => m.Contains("Attempting to download") && m.Contains("_framework/Json.") && m.Contains(".pdb")), "The lazy loading test didn't load PDB");
        }
    }

    [Fact]
    public async Task FailOnMissingLazyAssembly()
    {
        CopyTestAsset("WasmBasicTestApp", "LazyLoadingTests", "App");
        PublishProject("Debug");

        var result = await RunSdkStyleAppForPublish(new(
            Configuration: "Debug",
            TestScenario: "LazyLoadingTest",
            BrowserQueryString: new Dictionary<string, string> { ["loadRequiredAssembly"] = "false" },
            ExpectedExitCode: 1
        ));
        Assert.True(result.ConsoleOutput.Any(m => m.Contains("Could not load file or assembly") && m.Contains("Json")), "The lazy loading test didn't emit expected error message");
    }
}
