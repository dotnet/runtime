// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using SL = Microsoft.Build.Logging.StructuredLogger;

#nullable enable

namespace Wasm.Build.Tests;

public class DebugLevelTests : WasmTemplateTestsBase
{
    public DebugLevelTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    private void AssertDebugLevel(IReadOnlyCollection<string> result, int value)
        => Assert.Contains(result, m => m.Contains($"WasmDebugLevel: {value}"));

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task BuildWithDefaultLevel(Configuration configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: TestAsset.WasmBasicTestApp,
            idPrefix: "DebugLevelTests_BuildWithDefaultLevel"
        );
        BuildProject(info, configuration);
        BrowserRunOptions options = new(configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForBuildWithDotnetRun(options);
        AssertDebugLevel(result.TestOutput, -1);
    }

    [Theory]
    [InlineData(Configuration.Debug, 1)]
    [InlineData(Configuration.Release, 1)]
    [InlineData(Configuration.Debug, 0)]
    [InlineData(Configuration.Release, 0)]
    public async Task BuildWithExplicitValue(Configuration configuration, int debugLevel)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: TestAsset.WasmBasicTestApp,
            idPrefix: "DebugLevelTests_BuildWithExplicitValue"
        );
        BuildProject(info, configuration, new BuildOptions(ExtraMSBuildArgs: $"-p:WasmDebugLevel={debugLevel}"));
        BrowserRunOptions options = new(configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForBuildWithDotnetRun(options);
        AssertDebugLevel(result.TestOutput, debugLevel);
    }

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task PublishWithDefaultLevel(Configuration configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: TestAsset.WasmBasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithDefaultLevel"
        );
        PublishProject(info, configuration);
        BrowserRunOptions options = new(configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, 0);
    }

    [Theory]
    [InlineData(Configuration.Debug, 1)]
    [InlineData(Configuration.Release, 1)]
    [InlineData(Configuration.Debug, -1)]
    [InlineData(Configuration.Release, -1)]
    public async Task PublishWithExplicitValue(Configuration configuration, int debugLevel)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: TestAsset.WasmBasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithExplicitValue"
        );
        PublishProject(info, configuration, new PublishOptions(ExtraMSBuildArgs: $"-p:WasmDebugLevel={debugLevel}"));
        BrowserRunOptions options = new(configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, debugLevel);
    }


    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task PublishWithDefaultLevelAndPdbs(Configuration configuration)
    {
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: TestAsset.WasmBasicTestApp,
            idPrefix: "DebugLevelTests_PublishWithDefaultLevelAndPdbs"
        );
        PublishProject(info, configuration, new PublishOptions(ExtraMSBuildArgs: $"-p:CopyOutputSymbolsToPublishDirectory=true"));
        BrowserRunOptions options = new(configuration, TestScenario: "DebugLevelTest", ExpectedExitCode: 42);
        RunResult result = await RunForPublishWithWebServer(options);
        AssertDebugLevel(result.TestOutput, -1);
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
    [InlineData(false)]
    [InlineData(true)]
    [TestCategory("no-workload")]
    public void BuildAfterTrimmedPublishUsesMatchingSymbols(bool removeSymbols)
    {
        Configuration configuration = Configuration.Release;
        ProjectInfo info = CopyTestAsset(
            configuration,
            aot: false,
            asset: TestAsset.WasmBasicTestApp,
            idPrefix: "DebugLevelTests_TrimmedSymbols",
            extraProperties: $"""
                <PublishTrimmed>true</PublishTrimmed>
                <TrimmerRemoveSymbols>{removeSymbols.ToString().ToLowerInvariant()}</TrimmerRemoveSymbols>
                <CopyOutputSymbolsToPublishDirectory>true</CopyOutputSymbolsToPublishDirectory>
                <WasmDebugLevel>1</WasmDebugLevel>
                <WasmBuildNative>false</WasmBuildNative>
                """);

        PublishProject(info, configuration,
            new PublishOptions(UseCache: false, ExtraMSBuildArgs: "-p:CompressionEnabled=true"),
            isNativeBuild: false);
        string objDir = GetObjDir(configuration);
        string linkedDir = Path.Combine(objDir, "linked");
        string[] publishedSymbols = Directory.GetFiles(GetBinFrameworkDir(configuration, forPublish: true), "*.pdb*");
        if (removeSymbols)
        {
            Assert.Empty(publishedSymbols);
        }
        else
        {
            foreach (string assemblyName in new[] { info.ProjectName, "ResourceLibrary" })
            {
                byte[] expectedSymbols = File.ReadAllBytes(Path.Combine(linkedDir, $"{assemblyName}.pdb"));
                foreach (string suffix in new[] { "", ".gz", ".br" })
                {
                    string publishedPdb = Assert.Single(publishedSymbols,
                        path => Path.GetFileName(path).StartsWith($"{assemblyName}.", StringComparison.Ordinal) &&
                                path.EndsWith($".pdb{suffix}", StringComparison.Ordinal));
                    using FileStream file = File.OpenRead(publishedPdb);
                    using Stream content = suffix switch
                    {
                        ".gz" => new GZipStream(file, CompressionMode.Decompress),
                        ".br" => new BrotliStream(file, CompressionMode.Decompress),
                        _ => file
                    };
                    using MemoryStream decompressedSymbols = new();
                    content.CopyTo(decompressedSymbols);
                    Assert.Equal(expectedSymbols, decompressedSymbols.ToArray());
                }
            }
        }

        (_, string binlogPath) = BuildProjectWithoutAssert(configuration, info.ProjectName, _defaultBuildOptions with
        {
            ExtraMSBuildArgs = $"{_defaultBuildOptions.ExtraMSBuildArgs} -p:WasmBuildOnlyAfterPublish=true"
        });
        SL.Build build = SL.BinaryLog.ReadBuild(binlogPath);
        SL.BuildAnalyzer.AnalyzeBuild(build);
        SL.Task task = Assert.Single(build.FindChildrenRecursive<SL.Task>(task => task.Name == "ComputeWasmBuildAssets"));
        SL.NamedNode assets = task.FindChild<SL.Folder>("OutputItems").FindChild<SL.NamedNode>("_BuildAssetsCandidates");
        Assert.NotNull(assets);
        string[] symbols = assets.Children.OfType<SL.Item>()
            .Where(item => Path.GetExtension(item.Text) == ".pdb")
            .Select(item => Path.GetFullPath(item.Text, _projectDir))
            .ToArray();
        if (removeSymbols)
        {
            Assert.Empty(symbols);
        }
        else
        {
            Assert.Contains(Path.Combine(linkedDir, $"{info.ProjectName}.pdb"), symbols);
            Assert.Contains(Path.Combine(linkedDir, "ResourceLibrary.pdb"), symbols);
            Assert.All(symbols, path => Assert.Equal(Path.Combine(linkedDir, Path.GetFileName(path)), path));
        }
    }
}
