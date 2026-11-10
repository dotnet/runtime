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
    private static string ClientAssemblyExtension => UseWebcil ? ".wasm" : ".dll";

    public AssetCachingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext, DefaultTargetFrameworkForBlazorTemplate)
    {
    }

    [Theory]
    [InlineData("empty", false, true, 2, true, "assembly")]
    [InlineData("not-found", false, true, 2, true, "assembly")]
    [InlineData("integrity", false, true, 2, true, "assembly")]
    [InlineData("empty", true, true, 2, true, "assembly")]
    [InlineData("not-found", true, true, 2, true, "assembly")]
    [InlineData("empty,unavailable", false, true, 3, true, "assembly")]
    [InlineData("empty,empty,empty", false, true, 3, false, "assembly")]
    [InlineData("empty,empty,empty", true, true, 3, false, "assembly")]
    [InlineData("not-found,not-found", true, true, 2, false, "assembly")]
    [InlineData("empty", false, false, 1, false, "assembly")]
    [InlineData("empty", false, true, 2, true, "native")]
    [InlineData("not-found", true, true, 2, true, "native")]
    [InlineData("not-found", true, true, 2, true, "native-default")]
    [InlineData("empty", true, true, 2, true, "icu")]
    [InlineData("not-found", true, true, 2, true, "icu")]
    public Task BlazorApp_RetriesInvalidCachedAssets(
        string failures, bool disableIntegrityCheck,
        bool enableDownloadRetry, int expectedAttempts, bool expectSuccess, string assetKind)
        => RunInvalidCachedAssets(failures, disableIntegrityCheck, enableDownloadRetry, expectedAttempts, expectSuccess, assetKind);

    [Fact, TestCategory("no-fingerprinting")]
    public Task BlazorApp_RetryFailuresFollowFingerprintingConfiguration()
        => RunInvalidCachedAssets("empty,unavailable,unavailable,unavailable",
            disableIntegrityCheck: false, enableDownloadRetry: true, expectedAttempts: 3, expectSuccess: false, assetKind: "assembly");

    [Theory]
    [InlineData(null)]
    [InlineData("http://127.0.0.1:12345")]
    public void ServerEnvironmentPreservesExplicitUrls(string? urls)
    {
        var original = urls is null ? null : new Dictionary<string, string>
        {
            ["ASPNETCORE_URLS"] = urls,
            ["CACHE_TEST_SETTING"] = "preserved"
        };
        var environment = GetServerEnvironment(original);
        Assert.Equal(urls ?? "http://127.0.0.1:0", environment["ASPNETCORE_URLS"]);
        if (original is not null)
        {
            Assert.NotSame(original, environment);
            Assert.Equal(2, original.Count);
            Assert.Equal("preserved", environment["CACHE_TEST_SETTING"]);
        }
    }

    private async Task RunInvalidCachedAssets(
        string failures, bool disableIntegrityCheck,
        bool enableDownloadRetry, int expectedAttempts, bool expectSuccess, string assetKind)
    {
        bool fingerprintAssets = EnvironmentVariables.UseFingerprinting;
        var project = CopyTestAsset(
            Configuration.Release, aot: false, TestAsset.BlazorWebWasm,
            "CacheRecovery", appendUnicodeToPath: false);
        ConfigureClientRuntime();
        var publishOptions = new PublishOptions(
            TargetFramework: DefaultTargetFrameworkForBlazorTemplate,
            AssertAppBundle: false,
            ExtraMSBuildArgs: CacheTestPublishArgs);
        (string projectDir, string buildOutput) = BlazorPublish(project, Configuration.Release, publishOptions);
        string runtimePack = Assert.IsType<string>(ProjectProviderBase.TryGetRuntimePackDirFromBuildOutput(buildOutput));
        string packName = s_buildEnv.IsCoreClrRuntime
            ? "Microsoft.NETCore.App.Runtime.browser-wasm"
            : "Microsoft.NETCore.App.Runtime.Mono.browser-wasm";
        string packVersion = s_buildEnv.GetRuntimePackVersion(DefaultTargetFramework);
        Assert.EndsWith(Path.Combine(packName, packVersion), runtimePack, StringComparison.OrdinalIgnoreCase);
        string expectedLoaderPath = Path.Combine(s_buildEnv.GetRuntimeNativeDir(DefaultTargetFramework), "dotnet.js");
        string resolvedLoaderPath = Path.Combine(runtimePack, "runtimes", "browser-wasm", "native", "dotnet.js");
        byte[] expectedLoaderHash;
        if (File.Exists(expectedLoaderPath) && !string.Equals(
            Path.GetFullPath(expectedLoaderPath), Path.GetFullPath(resolvedLoaderPath),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            using Stream expectedLoader = File.OpenRead(expectedLoaderPath);
            expectedLoaderHash = SHA256.HashData(expectedLoader);
        }
        else
        {
            _testOutput.WriteLine($"No independent SDK-installed loader is available; verifying '{packName}' against the local package.");
            using var package = ZipFile.OpenRead(Path.Combine(s_buildEnv.BuiltNuGetsPath, $"{packName}.{packVersion}.nupkg"));
            var loader = package.GetEntry("runtimes/browser-wasm/native/dotnet.js");
            Assert.NotNull(loader);
            using Stream expectedLoader = loader.Open();
            expectedLoaderHash = SHA256.HashData(expectedLoader);
        }
        using Stream resolvedLoader = File.OpenRead(resolvedLoaderPath);
        Assert.Equal(expectedLoaderHash, SHA256.HashData(resolvedLoader));
        var buildPaths = GetBuildPaths(Configuration.Release, forPublish: true, projectDir: projectDir);
        string publishedAppPath = Path.Combine(buildPaths.BinDir, "publish");
        string frameworkPath = Path.Combine(publishedAppPath, "wwwroot", "_framework");
        (string assetPrefix, string assetPattern) = assetKind switch
        {
            "assembly" => ("BlazorWebWasm.Client", $"BlazorWebWasm.Client*{ClientAssemblyExtension}"),
            "native" or "native-default" => ("dotnet.native", "dotnet.native*.wasm"),
            "icu" => ("icudt", "icudt_EFIGS*.dat"),
            _ => throw new ArgumentException("Unknown boot resource kind.", nameof(assetKind))
        };
        bool nativeWasm = assetKind is "native" or "native-default";
        string assetName = Path.GetFileName(Assert.Single(Directory.EnumerateFiles(
            frameworkPath, assetPattern)));

        var counterLoaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startupFailed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runOptions = new BlazorRunOptions(Configuration.Release)
        {
            BrowserPath = $"cache-seed.html?asset={Uri.EscapeDataString(assetName)}",
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
            .WithEnvironmentVariables(GetServerEnvironment(runOptions.ServerEnvironment))
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
        (string projectDir, string output) = BlazorPublish(
            project,
            Configuration.Release,
            new PublishOptions(
                TargetFramework: DefaultTargetFrameworkForBlazorTemplate,
                AssertAppBundle: false,
                ExtraMSBuildArgs: CacheTestPublishArgs));

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
                var firstLoadWasmRequests = firstLoadRequestLogs.Where(log => log.Path.Contains("BlazorWebWasm.Client", StringComparison.Ordinal) && log.Path.EndsWith(ClientAssemblyExtension, StringComparison.Ordinal));

                // Check server request logs after the first load.
                Assert.NotEmpty(firstLoadWasmRequests);
                Assert.All(firstLoadWasmRequests, log => Assert.Equal(200, log.StatusCode));

                await requestLogClient.ClearRequestLogsAsync();
                counterLoaded = new();

                // Perform browser navigation to cause resource reload.
                await page.ReloadAsync();
                await counterLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));

                var secondLoadRequestLogs = await requestLogClient.GetRequestLogsAsync();
                var secondLoadWasmRequests = secondLoadRequestLogs.Where(log => log.Path.Contains("BlazorWebWasm.Client", StringComparison.Ordinal) && log.Path.EndsWith(ClientAssemblyExtension, StringComparison.Ordinal));

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
            .WithEnvironmentVariables(GetServerEnvironment(runOptions.ServerEnvironment));
        var result = await BrowserRun(cmd, $"exec {publishedAppDllFileName}", runOptions);
    }

    private void ConfigureClientRuntime()
    {
        string clientProjectDir = Path.GetFullPath(Path.Combine(_projectDir, "..", "BlazorWebWasm.Client"));
        string extraProperties = "", extraItems = "", insertAtEnd = "";
        AddCoreClrProjectProperties(ref extraProperties, ref extraItems, ref insertAtEnd);
        if (IsMonoRuntime)
        {
            string runtimePackVersion = s_buildEnv.GetRuntimePackVersion(DefaultTargetFramework);
            extraProperties +=
                $$"""
                <UseMonoRuntime>true</UseMonoRuntime>
                <WBTMonoRuntimePackVersion>{{runtimePackVersion}}</WBTMonoRuntimePackVersion>
                """;
            File.Copy(
                BuildEnvironment.WasmOverridePacksTargetsPath,
                Path.Combine(clientProjectDir, "WasmOverridePacks.targets"),
                overwrite: true);
            insertAtEnd += """<Import Project="WasmOverridePacks.targets" />""";
        }
        UpdateProjectFile(
            Path.Combine(clientProjectDir, "BlazorWebWasm.Client.csproj"),
            runAnalyzers: true, extraProperties, extraItems, insertAtEnd);
    }
}
