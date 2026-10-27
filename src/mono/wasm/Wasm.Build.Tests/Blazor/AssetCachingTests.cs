// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class AssetCachingTests : BlazorWasmTestBase
{
    private const string CacheTestPublishArgs = "-p:PublishReadyToRun=false -p:WasmBuildNative=false";

    public AssetCachingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData(true, "empty", false, true, 2, true, "assembly")]
    [InlineData(true, "not-found", false, true, 2, true, "assembly")]
    [InlineData(true, "integrity", false, true, 2, true, "assembly")]
    [InlineData(true, "empty", true, true, 2, true, "assembly")]
    [InlineData(true, "not-found", true, true, 2, true, "assembly")]
    [InlineData(true, "empty,unavailable", false, true, 3, true, "assembly")]
    [InlineData(true, "empty,empty,empty", false, true, 3, false, "assembly")]
    [InlineData(true, "empty,empty,empty", true, true, 3, false, "assembly")]
    [InlineData(true, "not-found,not-found", true, true, 2, false, "assembly")]
    [InlineData(true, "empty", false, false, 1, false, "assembly")]
    [InlineData(false, "empty,unavailable,unavailable,unavailable", false, true, 3, false, "assembly")]
    [InlineData(true, "empty", false, true, 2, true, "native")]
    [InlineData(true, "not-found", true, true, 2, true, "native")]
    [InlineData(true, "not-found", true, true, 2, true, "native-default")]
    [InlineData(true, "empty", true, true, 2, true, "icu")]
    [InlineData(true, "not-found", true, true, 2, true, "icu")]
    public async Task BlazorApp_RetriesInvalidCachedAssets(
        bool fingerprintAssets, string failures, bool disableIntegrityCheck,
        bool enableDownloadRetry, int expectedAttempts, bool expectSuccess, string assetKind)
    {
        var project = CopyTestAsset(
            Configuration.Release, aot: false, TestAsset.BlazorWebWasm,
            "CacheRecovery", appendUnicodeToPath: false);
        ConfigureClientRuntime();
        var publishOptions = new PublishOptions(
            AssertAppBundle: false,
            ExtraMSBuildArgs: CacheTestPublishArgs,
            ExtraBuildEnvironmentVariables: new Dictionary<string, string>
            {
                ["WasmFingerprintAssets"] = fingerprintAssets.ToString()
            });
        (string projectDir, string buildOutput) = BlazorPublish(project, Configuration.Release, publishOptions);
        string runtimePack = Assert.IsType<string>(ProjectProviderBase.TryGetRuntimePackDirFromBuildOutput(buildOutput));
        string packName = s_buildEnv.IsCoreClrRuntime
            ? "Microsoft.NETCore.App.Runtime.browser-wasm"
            : "Microsoft.NETCore.App.Runtime.Mono.browser-wasm";
        string packVersion = s_buildEnv.GetRuntimePackVersion(DefaultTargetFramework);
        Assert.EndsWith(Path.Combine(packName, packVersion), runtimePack, StringComparison.OrdinalIgnoreCase);
        using (var package = ZipFile.OpenRead(Path.Combine(s_buildEnv.BuiltNuGetsPath, $"{packName}.{packVersion}.nupkg")))
        {
            var loader = package.GetEntry("runtimes/browser-wasm/native/dotnet.js");
            Assert.NotNull(loader);
            using Stream expectedLoader = loader.Open();
            using Stream actualLoader = File.OpenRead(Path.Combine(runtimePack, "runtimes", "browser-wasm", "native", "dotnet.js"));
            Assert.Equal(SHA256.HashData(expectedLoader), SHA256.HashData(actualLoader));
        }
        var buildPaths = GetBuildPaths(Configuration.Release, forPublish: true, projectDir: projectDir);
        string publishedAppPath = Path.Combine(buildPaths.BinDir, "publish");
        (string assetPrefix, string assetPattern) = assetKind switch
        {
            "assembly" => ("BlazorWebWasm.Client", "BlazorWebWasm.Client*.wasm"),
            "native" or "native-default" => ("dotnet.native", "dotnet.native*.wasm"),
            "icu" => ("icudt", "icudt_EFIGS*.dat"),
            _ => throw new ArgumentException("Unknown boot resource kind.", nameof(assetKind))
        };
        bool nativeWasm = assetKind is "native" or "native-default";
        string assetName = Path.GetFileName(Assert.Single(Directory.EnumerateFiles(
            Path.Combine(publishedAppPath, "wwwroot", "_framework"), assetPattern)));

        var counterLoaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startupFailed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runOptions = new BlazorRunOptions(Configuration.Release)
        {
            BrowserPath = $"cache-seed?asset={Uri.EscapeDataString(assetName)}",
            DetectRuntimeFailures = expectSuccess,
            CheckCounter = false,
            ExpectedExitCode = expectSuccess ? 0 : null,
            OnConsoleMessage = (type, message) =>
            {
                if (message.Contains("Counter.OnAfterRender", StringComparison.Ordinal))
                {
                    counterLoaded.TrySetResult();
                }
                if (type is "error")
                {
                    ObserveStartupError(message);
                }
            },
            OnErrorMessage = ObserveStartupError,
            Test = async page =>
            {
                await page.WaitForFunctionAsync("document.title === 'cache-seeded'");
                string baseUrl = new Uri(page.Url).GetLeftPart(UriPartial.Authority);
                using var requestLogClient = new BlazorWebWasmLogClient(baseUrl);
                var seedRequests = await requestLogClient.GetRequestLogsAsync();
                string assetPath = Assert.Single(seedRequests.Where(log => log.Path.EndsWith(assetName, StringComparison.Ordinal))).Path;

                await page.AddInitScriptAsync(
                    $$"""
                    globalThis.cacheTestConfig = {
                        {{(assetKind is "native" ? "maxParallelDownloads: 1," : "")}}
                        disableIntegrityCheck: {{disableIntegrityCheck.ToString().ToLowerInvariant()}},
                        enableDownloadRetry: {{enableDownloadRetry.ToString().ToLowerInvariant()}}
                    };
                    globalThis.cacheTestRequests = [];
                    globalThis.cacheTestProgress = [];
                    const originalFetch = globalThis.fetch;
                    globalThis.fetch = (input, options) => {
                        const url = input instanceof Request ? input.url : String(input);
                        if (url.includes({{System.Text.Json.JsonSerializer.Serialize(assetName)}})) {
                            globalThis.cacheTestRequests.push(options?.cache);
                        }
                        return originalFetch(input, options);
                    };
                    """);

                await page.GotoAsync($"{baseUrl}/counter");
                await Task.WhenAny(counterLoaded.Task, startupFailed.Task).WaitAsync(TimeSpan.FromSeconds(60));

                string[] cacheModes = await page.EvaluateAsync<string[]>("globalThis.cacheTestRequests");
                Assert.Equal(expectedAttempts, cacheModes.Length);
                Assert.Equal(fingerprintAssets ? "force-cache" : "no-cache", cacheModes[0]);
                Assert.All(cacheModes.Skip(1), mode => Assert.Equal(fingerprintAssets ? "reload" : "no-cache", mode));
                Assert.Equal(expectSuccess, counterLoaded.Task.IsCompleted);
                Assert.Equal(!expectSuccess, startupFailed.Task.IsCompleted);
                int[][] progress = await page.EvaluateAsync<int[][]>("globalThis.cacheTestProgress");
                if (expectSuccess)
                {
                    Assert.NotEmpty(progress);
                }
                Assert.All(progress, counts =>
                {
                    Assert.Equal(2, counts.Length);
                    Assert.InRange(counts[0], 0, counts[1]);
                });

                var requests = await requestLogClient.GetRequestLogsAsync();
                var assetRequests = requests.Where(log => log.Path == assetPath).ToArray();
                Assert.Equal(fingerprintAssets ? expectedAttempts : expectedAttempts + 1, assetRequests.Length);
                if (expectSuccess)
                {
                    Assert.Equal(200, assetRequests[^1].StatusCode);
                    await requestLogClient.ClearRequestLogsAsync();
                    counterLoaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    await page.ReloadAsync();
                    await counterLoaded.Task.WaitAsync(TimeSpan.FromSeconds(60));
                    requests = await requestLogClient.GetRequestLogsAsync();
                    cacheModes = await page.EvaluateAsync<string[]>("globalThis.cacheTestRequests");
                    Assert.Equal(["force-cache"], cacheModes);
                    var warmRequests = requests.Where(log => log.Path == assetPath).ToArray();
                    if (nativeWasm)
                    {
                        Assert.InRange(warmRequests.Length, 0, 1);
                        Assert.All(warmRequests, log => Assert.Equal(200, log.StatusCode));
                    }
                    else
                    {
                        Assert.Empty(warmRequests);
                    }
                }

                await page.EvaluateAsync("console.log('WASM EXIT 0');");
            }
        };

        using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(publishedAppPath)
            .WithEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:0")
            .WithEnvironmentVariable("BLAZOR_CACHE_TEST_FAILURES", failures)
            .WithEnvironmentVariable("BLAZOR_CACHE_TEST_ASSET", assetName);
        await BrowserRun(cmd, $"exec {project.ProjectName}.dll", runOptions);

        void ObserveStartupError(string message)
        {
            if (message.Contains("Failed to start platform", StringComparison.Ordinal) ||
                (message.Contains(assetPrefix, StringComparison.Ordinal) &&
                (message.Contains("Error in mono_download_assets:", StringComparison.Ordinal) ||
                 message.Contains("Failed to load Webcil module '", StringComparison.Ordinal) ||
                 message.Contains("Failed to load resource '", StringComparison.Ordinal))))
            {
                startupFailed.TrySetResult();
            }
        }
    }

    [Fact, TestCategory("no-fingerprinting")]
    public async Task BlazorApp_BasedOnFingerprinting_LoadsWasmAssetsFromCache()
    {
        var project = CopyTestAsset(
            Configuration.Release,
            aot: false,
            TestAsset.BlazorWebWasm,
            "AssetCachingTest",
            appendUnicodeToPath: false
        );

        ConfigureClientRuntime();
        (string projectDir, string output) = BlazorPublish(project, Configuration.Release,
            new PublishOptions(AssertAppBundle: false, ExtraMSBuildArgs: CacheTestPublishArgs));

        var counterLoaded = new TaskCompletionSource();

        var runOptions = new BlazorRunOptions(Configuration.Release)
        {
            OnConsoleMessage = (type, msg) =>
            {
                if (msg.Contains("Counter.OnAfterRender"))
                    counterLoaded.SetResult();
            },
            Test = async (page) =>
            {
                var baseUrl = page.Url;

                await page.GotoAsync($"{baseUrl}counter");
                await counterLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));

                using var requestLogClient = new BlazorWebWasmLogClient(baseUrl);
                var firstLoadRequestLogs = await requestLogClient.GetRequestLogsAsync();
                var firstLoadWasmRequests = firstLoadRequestLogs.Where(log => log.Path.Contains("BlazorWebWasm.Client", StringComparison.Ordinal) && log.Path.EndsWith(".wasm"));

                // Check server request logs after the first load.
                Assert.NotEmpty(firstLoadWasmRequests);
                Assert.All(firstLoadWasmRequests, log => Assert.Equal(200, log.StatusCode));

                await requestLogClient.ClearRequestLogsAsync();
                counterLoaded = new();

                // Perform browser navigation to cause resource reload.
                await page.ReloadAsync();
                await counterLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));

                var secondLoadRequestLogs = await requestLogClient.GetRequestLogsAsync();
                var secondLoadWasmRequests = secondLoadRequestLogs.Where(log => log.Path.Contains("BlazorWebWasm.Client", StringComparison.Ordinal) && log.Path.EndsWith(".wasm"));

                // Check server logs after the second load.
                if (EnvironmentVariables.UseFingerprinting)
                {
                    // Large runtime binaries may be evicted; verify cache reuse with the client assembly.
                    Assert.Empty(secondLoadWasmRequests);
                }
                else
                {
                    // Without fingerprinting we should see validation requests for Wasm assets
                    // during the second load with response status 304.
                    Assert.NotEmpty(secondLoadWasmRequests);
                    Assert.All(secondLoadWasmRequests, log => Assert.Equal(304, log.StatusCode));
                }

                await page.EvaluateAsync("console.log('WASM EXIT 0');");
            }
        };

        var buildPaths = GetBuildPaths(Configuration.Release, forPublish: true, projectDir: projectDir);
        var publishedAppPath = Path.Combine(buildPaths.BinDir, "publish");
        var publishedAppDllFileName = $"{project.ProjectName}.dll";
        using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(publishedAppPath)
            .WithEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:0");
        var result = await BrowserRun(cmd, $"exec {publishedAppDllFileName}", runOptions);
    }

    private void ConfigureClientRuntime()
    {
        string extraProperties = "", extraItems = "", insertAtEnd = "";
        AddCoreClrProjectProperties(ref extraProperties, ref extraItems, ref insertAtEnd);
        UpdateProjectFile(
            Path.GetFullPath(Path.Combine(_projectDir, "..", "BlazorWebWasm.Client", "BlazorWebWasm.Client.csproj")),
            runAnalyzers: true, extraProperties, extraItems, insertAtEnd);
    }
}
