// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class ModuleConfigTests : WasmTemplateTestsBase
{
    public ModuleConfigTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DownloadProgressFinishes(bool failAssemblyDownload)
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, $"ModuleConfigTests_DownloadProgressFinishes_{failAssemblyDownload}");
        PublishProject(info, config);

        var result = await RunForPublishWithWebServer(new BrowserRunOptions(
            Configuration: config,
            TestScenario: "DownloadResourceProgressTest",
            BrowserQueryString: new NameValueCollection { {"failAssemblyDownload", failAssemblyDownload.ToString().ToLowerInvariant() } }
        ));
        Assert.True(
            result.TestOutput.Any(m => m.Contains("DownloadResourceProgress: Finished")),
            "The download progress test didn't emit expected error message"
        );
        Assert.True(
            result.ConsoleOutput.Any(m => m.Contains("Retrying download")) == failAssemblyDownload,
            failAssemblyDownload
                ? "The download progress test didn't emit expected message about retrying download"
                : "The download progress test did emit unexpected message about retrying download"
        );
        Assert.False(
            result.ConsoleOutput.Any(m => m.Contains("Retrying download (2)")),
            "The download progress test did emit unexpected message about second download retry"
        );
        Assert.True(
            result.TestOutput.Any(m => m.Contains("Throw error instead of downloading resource")) == failAssemblyDownload,
            failAssemblyDownload
                ? "The download progress test didn't emit expected message about failing download"
                : "The download progress test did emit unexpected message about failing download"
        );
    }

    [Fact]
    public async Task DownloadRetryRecoversFromFailure()
    {
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "ModuleConfigTests_DownloadRetryRecoversFromFailure");
        PublishProject(info, config);

        var result = await RunForPublishWithWebServer(new BrowserRunOptions(
            Configuration: config,
            TestScenario: "DownloadResourceProgressTest",
            BrowserQueryString: new NameValueCollection { {"failAssemblyDownload", "true" } }
        ));
        Assert.True(
            result.TestOutput.Any(m => m.Contains("DownloadResourceProgress: Finished")),
            "Download progress didn't finish after retries"
        );
        Assert.True(
            result.ConsoleOutput.Any(m => m.Contains("Retrying download")),
            "Expected retry log message was not emitted"
        );
        Assert.False(
            result.ConsoleOutput.Any(m => m.Contains("Retrying download (2)")),
            "Second retry should not be needed since first retry succeeds"
        );
    }

    [Fact, TestCategory("bundler-friendly")]
    public async Task OutErrOverrideWorks()
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "ModuleConfigTests_OutErrOverrideWorks");
        PublishProject(info, config);

        var result = await RunForPublishWithWebServer(new BrowserRunOptions(
            Configuration: Configuration.Debug,
            TestScenario: "OutErrOverrideWorks"
        ));
        Assert.True(
            result.ConsoleOutput.Any(m => m.Contains("Emscripten out override works!")),
            "Emscripten out override doesn't work"
        );
        Assert.True(
            result.ConsoleOutput.Any(m => m.Contains("Emscripten err override works!")),
            "Emscripten err override doesn't work"
        );
    }

    [Theory]
    [InlineData("not-found", false)]
    [InlineData("network", false)]
    [InlineData("not-found", true)]
    [InlineData("network", true)]
    [InlineData("none", false)]
    [TestCategory("coreclr"), TestCategory("native"), TestCategory("no-workload")]
    public async Task NativeSymbolsDownloadDoesNotHang(string failure, bool ignorePdbLoadErrors)
    {
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(
            config, aot: false, TestAsset.WasmBasicTestApp,
            $"NativeSymbolsDownload_{failure}_{ignorePdbLoadErrors}",
            extraProperties: "<WasmEmitSymbolMap>true</WasmEmitSymbolMap><WasmBuildNative>true</WasmBuildNative><PublishReadyToRun>false</PublishReadyToRun>");
        PublishProject(info, config, new PublishOptions(
            AssertAppBundle: false,
            ExtraMSBuildArgs: "-p:RuntimeFlavor=CoreCLR -p:WasmFingerprintAssets=true"));

        // The .NET 11 SDK does not stage newly linked CoreCLR symbol maps into its boot resources.
        // Supply the emitted map through the runtime configuration rather than backporting SDK changes.
        string symbolsPath = Path.Combine(_projectDir, "obj", config.ToString(), DefaultTargetFramework, "wasm", "for-publish", "dotnet.native.js.symbols");
        byte[] symbolsHash = SHA256.HashData(File.ReadAllBytes(symbolsPath));
        string symbolsName = $"dotnet.native.{Convert.ToHexString(symbolsHash).ToLowerInvariant()}.js.symbols";
        File.Copy(symbolsPath, Path.Combine(GetBinFrameworkDir(config, forPublish: true), symbolsName));

        bool expectSuccess = failure is "none" || (failure is "not-found" && ignorePdbLoadErrors);
        var result = await RunForPublishWithWebServer(new BrowserRunOptions(
            Configuration: config,
            TestScenario: "NativeSymbolsDownload",
            DetectRuntimeFailures: expectSuccess,
            ExpectedExitCode: expectSuccess ? 0 : 1,
            BrowserQueryString: new NameValueCollection
            {
                { "failure", failure },
                { "symbolsName", symbolsName },
                { "symbolsHash", Uri.EscapeDataString($"sha256-{Convert.ToBase64String(symbolsHash)}") },
                { "ignorePdbLoadErrors", ignorePdbLoadErrors.ToString().ToLowerInvariant() }
            }));

        Assert.Equal("Native symbols fetch: force-cache", Assert.Single(
            result.TestOutput, message => message.StartsWith("Native symbols fetch:", StringComparison.Ordinal)));
        Assert.Equal(expectSuccess, result.TestOutput.Contains("Native symbols startup completed"));
        if (!expectSuccess)
        {
            Assert.Contains(result.ConsoleOutput, message => message.Contains("Failed to load resource 'dotnet.native.", StringComparison.Ordinal));
        }
    }

    [Fact, TestCategory("bundler-friendly")]
    public async Task AssetIntegrity()
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, $"AssetIntegrity");
        PublishProject(info, config);

        var result = await RunForPublishWithWebServer(new BrowserRunOptions(
            Configuration: config,
            TestScenario: "AssetIntegrity"
        ));
        Assert.False(
            result.TestOutput.Any(m => !m.Contains(".js") && !m.Contains(".json") && m.Contains("has integrity ''")),
            "There are assets without integrity hash"
        );
    }

    [Fact]
    public async Task BufferedAssetsTest()
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(
            config,
            aot: false,
            TestAsset.WasmBasicTestApp,
            "ModuleConfigTests_BufferedAssetsTest",
            extraProperties: "<WasmEmitSymbolMap>true</WasmEmitSymbolMap>");
        PublishProject(info, config, new PublishOptions(AssertAppBundle: false));
        await RunForPublishWithWebServer(new BrowserRunOptions(
            Configuration: config,
            TestScenario: "BufferedAssetsTest"
        ));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [TestCategory("native-mono")]
    public void SymbolMapFileEmitted(bool isPublish)
        => SymbolMapFileEmittedCore(emitSymbolMap: true, isPublish);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SymbolMapFileNotEmitted(bool isPublish)
        => SymbolMapFileEmittedCore(emitSymbolMap: false, isPublish);

    private void SymbolMapFileEmittedCore(bool emitSymbolMap, bool isPublish)
    {
        Configuration config = Configuration.Release;
        string extraProperties = $"<WasmEmitSymbolMap>{emitSymbolMap.ToString().ToLowerInvariant()}</WasmEmitSymbolMap>";
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp,
            $"SymbolMapFile_{emitSymbolMap}_{isPublish}", extraProperties: extraProperties);

        if (isPublish)
            PublishProject(info, config, new PublishOptions(AssertAppBundle: false));
        else
            BuildProject(info, config, new BuildOptions(AssertAppBundle: false));

        // Locate the emitted symbols file. With CopyToOutputDirectory=Never, framework files are
        // no longer in bin/_framework during build: the native symbols file lives in
        // obj/{config}/{tfm}/wasm/for-build/ (native rebuild) and the materialized copy ends up
        // in obj/{config}/{tfm}/fx/{name}/_framework/. The publish path still has them in
        // bin/{config}/{tfm}/publish/wwwroot/_framework/.
        // The file may be fingerprinted (e.g. dotnet.native.<hash>.js.symbols), so use a glob.
        const string symbolsPattern = "dotnet.native*.js.symbols";
        bool symbolsFileExists;
        if (isPublish)
        {
            string frameworkDir = GetBinFrameworkDir(config, forPublish: true);
            symbolsFileExists = Directory.EnumerateFiles(frameworkDir, symbolsPattern).Any();
        }
        else
        {
            string objDir = Path.Combine(_projectDir, "obj", config.ToString(), DefaultTargetFramework);
            string fxBaseDir = Path.Combine(objDir, "fx");
            string[] searchDirs = [
                Path.Combine(objDir, "wasm", "for-build"),
                .. Directory.Exists(fxBaseDir)
                    ? Directory.GetDirectories(fxBaseDir).Select(d => Path.Combine(d, "_framework"))
                    : Array.Empty<string>()
            ];
            symbolsFileExists = searchDirs
                .Where(Directory.Exists)
                .Any(d => Directory.EnumerateFiles(d, symbolsPattern).Any());
        }
        Assert.Equal(emitSymbolMap, symbolsFileExists);
    }
}
