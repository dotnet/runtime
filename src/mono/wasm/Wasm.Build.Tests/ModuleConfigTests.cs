// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.NET.Sdk.WebAssembly;
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
    [TestCategory("native")]
    public void SymbolMapFileEmitted(bool isPublish)
        => SymbolMapFileEmittedCore(emitSymbolMap: true, isPublish);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SymbolMapFileNotEmitted(bool isPublish)
        => SymbolMapFileEmittedCore(emitSymbolMap: false, isPublish);

    [Fact]
    [TestCategory("coreclr")]
    public void RuntimePackSymbolMapMatchesFinalWasm()
    {
        (byte[] wasmBytes, byte[] symbolsBytes) = ReadRuntimePackNativeSymbols();
        NativeWasmSymbolMapInfo info = NativeWasmSymbolMapValidator.Validate(wasmBytes, symbolsBytes);

        Assert.True(info.ImportedFunctionCount > 0);
        Assert.Equal(info.DefinedFunctionCount, info.CodeFunctionCount);
        Assert.Equal(info.ImportedFunctionCount + info.DefinedFunctionCount, info.Symbols.Count);
        Assert.Equal(
            "InterpExecMethod(InterpreterFrame*, InterpMethodContextFrame*, InterpThreadContext*, ExceptionClauseArgs*)",
            Assert.Single(info.Symbols, entry => entry.Value.StartsWith("InterpExecMethod(", StringComparison.Ordinal)).Value);
        Assert.Contains(info.Symbols, entry => entry.Value == "ExecuteInterpretedMethod");
        Assert.Contains(
            info.Symbols,
            entry => entry.Value.StartsWith("ExecuteInterpretedMethodWithArgs_PortableEntryPoint(", StringComparison.Ordinal));
        Assert.Contains(info.Symbols, entry => Regex.IsMatch(entry.Value, @"^non-virtual thunk to .+_\d+$"));
        Assert.DoesNotContain(info.Symbols, entry => entry.Value.Contains("WasmR2RToInterpreterThunk", StringComparison.Ordinal));
        Assert.DoesNotContain(info.Symbols, entry => entry.Value.Contains("WasmInterpreterToR2RThunk", StringComparison.Ordinal));

        int browserHostIndex = info.FunctionExports["BrowserHost_InitializeDotnet"];
        Assert.Equal("BrowserHost_InitializeDotnet", info.Symbols[browserHostIndex]);

        int mallocIndex = info.FunctionExports["malloc"];
        Assert.Equal("emscripten_builtin_malloc", info.Symbols[mallocIndex]);
    }

    [Fact]
    [TestCategory("coreclr")]
    public void RuntimePackSymbolMapRejectsIndexAndIdentityMismatches()
    {
        (byte[] wasmBytes, byte[] symbolsBytes) = ReadRuntimePackNativeSymbols();
        string wasmIntegrity = NativeWasmSymbolMapValidator.ComputeIntegrity(wasmBytes);
        string symbolsIntegrity = NativeWasmSymbolMapValidator.ComputeIntegrity(symbolsBytes);

        byte[] shiftedSymbols = Encoding.UTF8.GetBytes(
            string.Join(
                Environment.NewLine,
                Encoding.UTF8.GetString(symbolsBytes)
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line =>
                    {
                        int separator = line.IndexOf(':');
                        int index = int.Parse(line.AsSpan(0, separator));
                        return $"{index + 1}:{line[(separator + 1)..].TrimEnd('\r')}";
                    })) +
            Environment.NewLine);

        InvalidDataException shiftedException = Assert.Throws<InvalidDataException>(
            () => NativeWasmSymbolMapValidator.Validate(
                wasmBytes,
                shiftedSymbols,
                wasmIntegrity,
                NativeWasmSymbolMapValidator.ComputeIntegrity(shiftedSymbols)));
        Assert.Contains("expected absolute function index 0", shiftedException.Message);

        byte[] staleWasm = (byte[])wasmBytes.Clone();
        staleWasm[^1] ^= 1;
        InvalidDataException staleWasmException = Assert.Throws<InvalidDataException>(
            () => NativeWasmSymbolMapValidator.Validate(staleWasm, symbolsBytes, wasmIntegrity, symbolsIntegrity));
        Assert.Contains("Wasm SHA-256 mismatch", staleWasmException.Message);

        byte[] staleSymbols = (byte[])symbolsBytes.Clone();
        staleSymbols[^2] ^= 1;
        InvalidDataException staleSymbolsException = Assert.Throws<InvalidDataException>(
            () => NativeWasmSymbolMapValidator.Validate(wasmBytes, staleSymbols, wasmIntegrity, symbolsIntegrity));
        Assert.Contains("symbol map SHA-256 mismatch", staleSymbolsException.Message);
    }

    private static (byte[] WasmBytes, byte[] SymbolsBytes) ReadRuntimePackNativeSymbols()
    {
        string runtimePackVersion = s_buildEnv.GetRuntimePackVersion(DefaultTargetFramework);
        string packagePath = Path.Combine(
            s_buildEnv.BuiltNuGetsPath,
            $"Microsoft.NETCore.App.Runtime.browser-wasm.{runtimePackVersion}.nupkg");

        using ZipArchive package = ZipFile.OpenRead(packagePath);
        return (
            ReadEntry("runtimes/browser-wasm/native/dotnet.native.wasm"),
            ReadEntry("runtimes/browser-wasm/native/dotnet.native.js.symbols"));

        byte[] ReadEntry(string entryName)
        {
            ZipArchiveEntry? entry = package.GetEntry(entryName);
            Assert.NotNull(entry);
            using Stream stream = entry.Open();
            using MemoryStream buffer = new(checked((int)entry.Length));
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
    }

    private void SymbolMapFileEmittedCore(bool emitSymbolMap, bool isPublish)
    {
        Configuration config = Configuration.Release;
        string extraProperties =
            $"<WasmEmitSymbolMap>{emitSymbolMap.ToString().ToLowerInvariant()}</WasmEmitSymbolMap>" +
            "<WasmBuildNative>false</WasmBuildNative>";

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
        string? symbolsFile;
        if (isPublish)
        {
            string frameworkDir = GetBinFrameworkDir(config, forPublish: true);
            symbolsFile = Directory.EnumerateFiles(frameworkDir, symbolsPattern).SingleOrDefault();
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
            symbolsFile = searchDirs
                .Where(Directory.Exists)
                .SelectMany(d => Directory.EnumerateFiles(d, symbolsPattern))
                .SingleOrDefault();
        }

        Assert.Equal(emitSymbolMap, symbolsFile is not null);
        if (!emitSymbolMap || !isPublish)
            return;

        string symbolsPath = Assert.IsType<string>(symbolsFile);
        string frameworkDirectory = GetBinFrameworkDir(config, forPublish: true);
        WasmSdkBasedProjectProvider provider = GetProvider<WasmSdkBasedProjectProvider>();
        BootJsonData bootJson = provider.GetBootJson(provider.GetBootConfigPath(frameworkDirectory));
        AssetsData assets = Assert.IsType<AssetsData>(bootJson.resources);
        SymbolsAsset symbolAsset = Assert.Single(assets.wasmSymbols);
        WasmAsset wasmAsset = Assert.Single(assets.wasmNative);

        Assert.Equal(Path.GetFileName(symbolsPath), symbolAsset.name);
        Assert.StartsWith("sha256-", symbolAsset.hash);
        Assert.StartsWith("sha256-", wasmAsset.hash);
        string wasmPath = Path.Combine(frameworkDirectory, wasmAsset.name);
        NativeWasmSymbolMapValidator.Validate(
            wasmPath,
            symbolsPath,
            expectedWasmIntegrity: wasmAsset.hash,
            expectedSymbolsIntegrity: symbolAsset.hash);
    }
}
