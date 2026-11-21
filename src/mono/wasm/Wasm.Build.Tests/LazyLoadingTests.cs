// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ILCompiler.Reflection.ReadyToRun;
using Internal.ReadyToRunConstants;
using Microsoft.NET.Sdk.WebAssembly;
using Microsoft.NET.WebAssembly.Webcil;
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
    [TestCategory("native"), TestCategory("mono")]
    public async Task LoadLazyAssemblyWithAOT()
    {
        // Regression coverage for https://github.com/dotnet/runtime/issues/125794:
        // AOT combined with BlazorWebAssemblyLazyLoad must still initialize the runtime
        // and lazily load assemblies at runtime. AOT is only supported when publishing
        // in Release, so this exercises the publish + AOT + lazy-load combination that
        // the Debug build/run tests above do not cover.
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, aot: true, TestAsset.WasmBasicTestApp, "LazyLoadingTestsAOT");
        PublishProject(info, config, new PublishOptions(AOT: true, ExtraMSBuildArgs: "-p:TestLazyLoading=true"));

        RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(
            config,
            AOT: true,
            TestScenario: "LazyLoadingTest"
        ));

        Assert.True(result.TestOutput.Any(m => m.Contains("FirstName")), "The lazy loading test didn't emit expected message with JSON");
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, true, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, true)]
    [TestCategory("no-workload")]
    public async Task PublishLoadsLazyAssemblies(bool trimmed, bool readyToRun, bool bundle, bool includeNoILLazyAssembly)
    {
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, $"r2r_lazy_{trimmed}_{readyToRun}_{bundle}_{includeNoILLazyAssembly}");
        ReplaceFile("LazyLoadingTest.cs", Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "CompositeLazyLoadingTest.cs"));
        UpdateFile("wwwroot/main.js", new Dictionary<string, string>
        {
            {
                "const firstJsonLoad = await INTERNAL.loadLazyAssembly(`Json${lazyAssemblyExtension}`);",
                """
                testOutput(`jsonAlreadyLoaded=${INTERNAL.getLoadedAssemblyBytes("Json.dll") !== undefined}`);
                const firstJsonLoad = await INTERNAL.loadLazyAssembly(`Json${lazyAssemblyExtension}`);
                testOutput(`jsonLoadedAfterLazy=${INTERNAL.getLoadedAssemblyBytes("Json.dll") !== undefined}`);
                """
            },
        });

        string args = $"{GetR2RBuildArgs(config, composite: true)} -p:TestLazyLoading=true -p:TestNoILLazyAssembly={includeNoILLazyAssembly.ToString().ToLowerInvariant()} -p:PublishReadyToRun={readyToRun.ToString().ToLowerInvariant()} -p:PublishReadyToRunComposite=true -p:PublishTrimmed={trimmed.ToString().ToLowerInvariant()} -p:WasmBundleCompositeComponents={bundle.ToString().ToLowerInvariant()} -p:WasmBuildNative=false";
        PublishProject(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: args));

        string frameworkDir = GetBinFrameworkDir(config, forPublish: true);
        WasmSdkBasedProjectProvider provider = GetProvider<WasmSdkBasedProjectProvider>();
        AssetsData assets = (AssetsData)provider.GetBootJson(provider.GetBootConfigPath(frameworkDir)).resources;
        if (readyToRun && bundle)
            Assert.Single(assets.coreAssembly, asset => asset.isBundle == true);
        else
            Assert.DoesNotContain(assets.coreAssembly, asset => asset.isBundle == true);
        if (includeNoILLazyAssembly)
        {
            WebcilAsset noIL = Assert.Single(assets.lazyAssembly, asset => asset.virtualPath == "NoILLazyLibrary.wasm");
            string noILPath = Path.Combine(frameworkDir, noIL.name);
            using FileStream stream = File.OpenRead(noILPath);
            Assert.True(WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out int tableSize, out string? reason), reason);
            Assert.Equal(0, tableSize);
        }
        WebcilAsset json = Assert.Single(assets.lazyAssembly, asset => asset.virtualPath == "Json.wasm");
        string jsonPath = Path.Combine(frameworkDir, json.name);
        using (FileStream stream = File.OpenRead(jsonPath))
        {
            Assert.True(WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out int tableSize, out string? reason), reason);
            if (readyToRun)
                Assert.True(tableSize > 0, "The lazy assembly must use a separate ReadyToRun image.");
            else
                Assert.Equal(0, tableSize);
        }

        if (readyToRun)
        {
            byte[] image = File.ReadAllBytes(jsonPath);
            var imageReader = new WebcilImageReader(image);
            Assert.True(imageReader.TryGetReadyToRunHeader(out int headerRva, out bool isCompositeOwner));
            Assert.False(isCompositeOwner);
            int headerOffset = imageReader.GetOffset(headerRva);
            Assert.Equal(ReadyToRunHeader.READYTORUN_SIGNATURE, BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(headerOffset)));
            const int FlagsOffset = sizeof(uint) + 2 * sizeof(ushort);
            var flags = (ReadyToRunFlags)BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(headerOffset + FlagsOffset));
            Assert.Equal((ReadyToRunFlags)0, flags & ReadyToRunFlags.READYTORUN_FLAG_Component);
        }

        NameValueCollection query = includeNoILLazyAssembly
            ? new NameValueCollection { { "loadNoILLazyAssembly", "true" } }
            : new NameValueCollection();
        RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "LazyLoadingTest", BrowserQueryString: query));
        Assert.Contains(result.TestOutput, output => output.Contains("jsonAlreadyLoaded=false"));
        Assert.Contains(result.TestOutput, output => output.Contains("firstJsonLoad=true"));
        Assert.Contains(result.TestOutput, output => output.Contains("jsonLoadedAfterLazy=true"));
        Assert.Contains(result.TestOutput, output => output.Contains("FirstName=Person"));
        if (includeNoILLazyAssembly)
        {
            Assert.Contains(result.TestOutput, output => output.Contains("noILLoad=true"));
            Assert.Contains(result.TestOutput, output => output.Contains("NoILType=NoILType"));
        }
    }

    [ConditionalFact(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
    [TestCategory("no-workload")]
    public void CompositeReadyToRunRejectsDirectLazyReference()
    {
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "r2r_lazy_reference");
        string args = $"{GetR2RBuildArgs(config, composite: true)} -p:TestLazyLoading=true -p:PublishReadyToRun=true -p:PublishReadyToRunComposite=true -p:PublishTrimmed=false -p:WasmBuildNative=false";
        (_, string output) = PublishProject(info, config, new PublishOptions(ExpectSuccess: false, ExtraMSBuildArgs: args));
        Assert.Contains("directly references lazy-loaded assembly 'Json'", output);
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
