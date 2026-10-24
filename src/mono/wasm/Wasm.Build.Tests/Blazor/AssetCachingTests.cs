// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class AssetCachingTests : BlazorWasmTestBase
{
    private const string InjectClientAssemblyFailureEnvironmentVariable = "BLAZOR_WEB_WASM_INJECT_CLIENT_ASSEMBLY_FAILURE";
    private const string ClientAssemblyFailureModeEnvironmentVariable = "BLAZOR_WEB_WASM_CLIENT_ASSEMBLY_FAILURE_MODE";

    public AssetCachingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact, TestCategory("no-workload")]
    public async Task BlazorApp_RecoversFromCachedEmptyFingerprintAsset()
    {
        var project = CopyTestAsset(
            Configuration.Release,
            aot: false,
            TestAsset.BlazorWebWasm,
            "CachedEmptyFingerprintAssetTest",
            appendUnicodeToPath: false
        );

        var publishOptions = new PublishOptions(
            AssertAppBundle: false,
            ExtraBuildEnvironmentVariables: new Dictionary<string, string>
            {
                ["WasmFingerprintAssets"] = "true",
            });
        (string projectDir, _) = BlazorPublish(project, Configuration.Release, publishOptions);

        var buildPaths = GetBuildPaths(Configuration.Release, forPublish: true, projectDir: projectDir);
        var publishedAppPath = Path.Combine(buildPaths.BinDir, "publish");
        var frameworkPath = Path.Combine(publishedAppPath, "wwwroot", "_framework");
        var clientAssemblyName = Path.GetFileName(Assert.Single(
            Directory.EnumerateFiles(frameworkPath, "BlazorWebWasm.Client*.wasm")));

        var counterLoaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runOptions = new BlazorRunOptions(Configuration.Release)
        {
            BrowserPath = $"cache-seed?asset={Uri.EscapeDataString(clientAssemblyName)}",
            OnConsoleMessage = (type, msg) =>
            {
                if (msg.Contains("Counter.OnAfterRender"))
                    counterLoaded.TrySetResult();
            },
            Test = async (page) =>
            {
                await page.WaitForFunctionAsync("document.title === 'cache-seeded'");

                var baseUrl = new Uri(page.Url).GetLeftPart(UriPartial.Authority);
                using var requestLogClient = new BlazorWebWasmLogClient(baseUrl);
                var seedLogs = await requestLogClient.GetRequestLogsAsync();
                var failedRequest = Assert.Single(seedLogs.Where(log => log.WasFailureInjected));

                await page.GotoAsync($"{baseUrl}/counter");
                await counterLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));

                var requestLogs = await requestLogClient.GetRequestLogsAsync();
                var clientAssemblyRequests = requestLogs
                    .Where(log => string.Equals(log.Path, failedRequest.Path, StringComparison.Ordinal))
                    .ToArray();

                Assert.Equal(2, clientAssemblyRequests.Length);
                Assert.All(clientAssemblyRequests, log => Assert.Equal(200, log.StatusCode));
                Assert.True(clientAssemblyRequests[0].WasFailureInjected);
                Assert.False(clientAssemblyRequests[1].WasFailureInjected);

                await requestLogClient.ClearRequestLogsAsync();
                counterLoaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
                await page.ReloadAsync();
                await counterLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));

                requestLogs = await requestLogClient.GetRequestLogsAsync();
                Assert.DoesNotContain(requestLogs, log => string.Equals(log.Path, failedRequest.Path, StringComparison.Ordinal));

                await page.EvaluateAsync("console.log('WASM EXIT 0');");
            }
        };

        var publishedAppDllFileName = $"{project.ProjectName}.dll";
        using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(publishedAppPath)
            .WithEnvironmentVariable(InjectClientAssemblyFailureEnvironmentVariable, "true");
        await BrowserRun(cmd, $"exec {publishedAppDllFileName}", runOptions);
    }

    [Theory, TestCategory("no-workload")]
    [InlineData("empty", 200)]
    [InlineData("error", 503)]
    [InlineData("integrity", 200)]
    public Task BlazorApp_DoesNotRetryAfterFailedCacheRecovery(string failureMode, int expectedStatusCode)
        => RunFailedCacheRecoveryTest(
            fingerprintAssets: true,
            failureMode: failureMode,
            expectedStatusCode: expectedStatusCode,
            expectedRequestCount: 2);

    [Fact, TestCategory("no-workload")]
    public Task BlazorApp_DoesNotUseCacheRecoveryForNonForceCache()
        => RunFailedCacheRecoveryTest(
            fingerprintAssets: false,
            failureMode: "empty",
            expectedStatusCode: 200,
            expectedRequestCount: 1);

    private async Task RunFailedCacheRecoveryTest(
        bool fingerprintAssets,
        string failureMode,
        int expectedStatusCode,
        int expectedRequestCount)
    {
        var project = CopyTestAsset(
            Configuration.Release,
            aot: false,
            TestAsset.BlazorWebWasm,
            $"CacheRecoveryLimitTest_{fingerprintAssets}_{failureMode}",
            appendUnicodeToPath: false
        );

        var publishOptions = new PublishOptions(
            AssertAppBundle: false,
            ExtraBuildEnvironmentVariables: new Dictionary<string, string>
            {
                ["WasmFingerprintAssets"] = fingerprintAssets.ToString(),
            });
        (string projectDir, _) = BlazorPublish(project, Configuration.Release, publishOptions);

        var counterLoaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startupFailed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runOptions = new BlazorRunOptions(Configuration.Release)
        {
            BrowserPath = "counter",
            OnConsoleMessage = (type, msg) =>
            {
                if (msg.Contains("Counter.OnAfterRender"))
                    counterLoaded.TrySetResult();
                if (type == "error" &&
                    msg.Contains("Error in mono_download_assets:", StringComparison.Ordinal) &&
                    msg.Contains("BlazorWebWasm.Client", StringComparison.OrdinalIgnoreCase))
                {
                    startupFailed.TrySetResult();
                }
            },
            Test = async (page) =>
            {
                await startupFailed.Task.WaitAsync(TimeSpan.FromSeconds(30));

                using var requestLogClient = new BlazorWebWasmLogClient(page.Url);
                var requestLogs = await requestLogClient.GetRequestLogsAsync();
                var failedRequests = requestLogs.Where(log => log.WasFailureInjected).ToArray();
                Assert.Equal(expectedRequestCount, failedRequests.Length);
                var failedRequest = failedRequests[0];
                var clientAssemblyRequests = requestLogs
                    .Where(log => string.Equals(log.Path, failedRequest.Path, StringComparison.Ordinal))
                    .ToArray();

                Assert.Equal(expectedRequestCount, clientAssemblyRequests.Length);
                Assert.All(clientAssemblyRequests, log =>
                {
                    Assert.Equal(expectedStatusCode, log.StatusCode);
                    Assert.True(log.WasFailureInjected);
                });
                Assert.False(counterLoaded.Task.IsCompleted);

                await page.EvaluateAsync("console.log('WASM EXIT 0');");
            }
        };

        var buildPaths = GetBuildPaths(Configuration.Release, forPublish: true, projectDir: projectDir);
        var publishedAppPath = Path.Combine(buildPaths.BinDir, "publish");
        var publishedAppDllFileName = $"{project.ProjectName}.dll";
        using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(publishedAppPath)
            .WithEnvironmentVariable(InjectClientAssemblyFailureEnvironmentVariable, expectedRequestCount.ToString())
            .WithEnvironmentVariable(ClientAssemblyFailureModeEnvironmentVariable, failureMode);
        await BrowserRun(cmd, $"exec {publishedAppDllFileName}", runOptions);
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

        (string projectDir, string output) = BlazorPublish(project, Configuration.Release, new PublishOptions(AssertAppBundle: false));

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
                var firstLoadWasmRequests = firstLoadRequestLogs.Where(log => log.Path.EndsWith(".wasm"));

                // Check server request logs after the first load.
                Assert.NotEmpty(firstLoadWasmRequests);
                Assert.All(firstLoadWasmRequests, log => Assert.Equal(200, log.StatusCode));

                await requestLogClient.ClearRequestLogsAsync();
                counterLoaded = new();

                // Perform browser navigation to cause resource reload.
                await page.ReloadAsync();
                await counterLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));

                var secondLoadRequestLogs = await requestLogClient.GetRequestLogsAsync();
                var secondLoadWasmRequests = secondLoadRequestLogs.Where(log => log.Path.EndsWith(".wasm"));

                // Check server logs after the second load.
                if (EnvironmentVariables.UseFingerprinting)
                {
                    // With fingerprinting we should not see any requests for Wasm assets during the second load.
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
        using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput).WithWorkingDirectory(publishedAppPath);
        var result = await BrowserRun(cmd, $"exec {publishedAppDllFileName}", runOptions);
    }
}
