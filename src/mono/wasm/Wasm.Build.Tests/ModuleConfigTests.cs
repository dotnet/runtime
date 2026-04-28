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

public class ModuleConfigTests : WasmTemplateTestsBase
{
    public ModuleConfigTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData(false)]
    // [InlineData(true)] // ActiveIssue: https://github.com/dotnet/runtime/issues/124946
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
            result.TestOutput.Any(m => m.Contains("Throw error instead of downloading resource") == failAssemblyDownload),
            failAssemblyDownload
                ? "The download progress test didn't emit expected message about failing download"
                : "The download progress test did emit unexpected message about failing download"
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [TestCategory("native")]
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
