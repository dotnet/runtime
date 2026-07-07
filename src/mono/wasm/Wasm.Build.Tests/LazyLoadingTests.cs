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

public class LazyLoadingTests : WasmTemplateTestsBase
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
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "LazyLoadingTests");
        BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: $"-p:LazyLoadingTestExtension={lazyLoadingTestExtension} -p:TestLazyLoading=true"));

        // We are running the app and passing all possible lazy extensions to test matrix of all possibilities.
        // We don't need to rebuild the application to test how client is trying to load the assembly.
        foreach (var clientLazyLoadingTestExtension in allLazyLoadingTestExtensions)
        {
            RunResult result = await RunForBuildWithDotnetRun(new BrowserRunOptions(
                config,
                TestScenario: "LazyLoadingTest",
                BrowserQueryString: new NameValueCollection { {"lazyLoadingTestExtension", clientLazyLoadingTestExtension } }
            ));

            Assert.True(result.TestOutput.Any(m => m.Contains("FirstName")), "The lazy loading test didn't emit expected message with JSON");
            Assert.True(result.ConsoleOutput.Any(m => m.Contains("Attempting to download") && m.Contains("_framework/Json.") && m.Contains(".pdb")), "The lazy loading test didn't load PDB");
        }
    }

    [Fact]
    public async Task LoadLazyAssemblyTwiceIsIdempotent()
    {
        // Regression test for the lazy loader rewriting an asset's virtualPath in place while
        // fetching it, which broke the lookup (and dedup) on a subsequent load of the same
        // assembly - e.g. when Blazor fires OnNavigate more than once - and surfaced as
        // "<assembly> must be marked with 'BlazorWebAssemblyLazyLoad' item group ...".
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "LazyLoadingTests");
        BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: "-p:TestLazyLoading=true"));

        RunResult result = await RunForBuildWithDotnetRun(new BrowserRunOptions(
            config,
            TestScenario: "LazyLoadingTest",
            BrowserQueryString: new NameValueCollection { { "loadLazyAssemblyTwice", "true" } }
        ));

        Assert.True(result.TestOutput.Any(m => m.Contains("firstJsonLoad=true")), "The first lazy load should report that it loaded the assembly");
        Assert.True(result.TestOutput.Any(m => m.Contains("secondJsonLoad=false")), "The second lazy load of the same assembly should be an idempotent no-op");
        Assert.True(result.TestOutput.Any(m => m.Contains("FirstName")), "The lazy loading test didn't emit expected message with JSON");
        Assert.False(result.ConsoleOutput.Any(m => m.Contains("must be marked with 'BlazorWebAssemblyLazyLoad'")), "Reloading an already-loaded lazy assembly must not throw the 'must be marked' error");
    }

    [Fact]
    public async Task FailOnMissingLazyAssembly()
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "LazyLoadingTests");

        PublishProject(info, config, new PublishOptions(ExtraMSBuildArgs: "-p:TestLazyLoading=true"));
        BrowserRunOptions options = new(
            config,
            TestScenario: "LazyLoadingTest",
            BrowserQueryString: new NameValueCollection { {"loadRequiredAssembly", "false" } },
            ExpectedExitCode: 1);
        RunResult result = await RunForPublishWithWebServer(options);
        Assert.True(result.ConsoleOutput.Any(m => m.Contains("Could not load file or assembly") && m.Contains("Json")), "The lazy loading test didn't emit expected error message");
    }
}
