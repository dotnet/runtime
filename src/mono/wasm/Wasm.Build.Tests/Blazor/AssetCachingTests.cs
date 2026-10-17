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
    private const string PoisonClientAssemblyEnvironmentVariable = "BLAZOR_WEB_WASM_POISON_CLIENT_ASSEMBLY";

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

        var counterLoaded = new TaskCompletionSource();
        var runOptions = new BlazorRunOptions(Configuration.Release)
        {
            BrowserPath = "counter",
            OnConsoleMessage = (type, msg) =>
            {
                if (msg.Contains("Counter.OnAfterRender"))
                    counterLoaded.TrySetResult();
            },
            Test = async (page) =>
            {
                await counterLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));

                using var requestLogClient = new BlazorWebWasmLogClient(page.Url);
                var requestLogs = await requestLogClient.GetRequestLogsAsync();
                var poisonedRequest = Assert.Single(requestLogs.Where(log => log.WasPoisoned));
                var clientAssemblyRequests = requestLogs
                    .Where(log => string.Equals(log.Path, poisonedRequest.Path, StringComparison.Ordinal))
                    .ToArray();

                Assert.Equal(2, clientAssemblyRequests.Length);
                Assert.All(clientAssemblyRequests, log => Assert.Equal(200, log.StatusCode));
                Assert.True(clientAssemblyRequests[0].WasPoisoned);
                Assert.False(clientAssemblyRequests[1].WasPoisoned);

                await page.EvaluateAsync("console.log('WASM EXIT 0');");
            }
        };

        var buildPaths = GetBuildPaths(Configuration.Release, forPublish: true, projectDir: projectDir);
        var publishedAppPath = Path.Combine(buildPaths.BinDir, "publish");
        var publishedAppDllFileName = $"{project.ProjectName}.dll";
        using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(publishedAppPath)
            .WithEnvironmentVariable(PoisonClientAssemblyEnvironmentVariable, "true");
        await BrowserRun(cmd, $"exec {publishedAppDllFileName}", runOptions);
    }

    [Theory, TestCategory("no-workload")]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public async Task BlazorApp_DoesNotRetryAfterRecoveryOrForNonForceCache(bool fingerprintAssets, int poisonCount)
    {
        var project = CopyTestAsset(
            Configuration.Release,
            aot: false,
            TestAsset.BlazorWebWasm,
            $"CacheRecoveryLimitTest_{fingerprintAssets}",
            appendUnicodeToPath: false
        );

        var publishOptions = new PublishOptions(
            AssertAppBundle: false,
            ExtraBuildEnvironmentVariables: new Dictionary<string, string>
            {
                ["WasmFingerprintAssets"] = fingerprintAssets.ToString(),
            });
        (string projectDir, _) = BlazorPublish(project, Configuration.Release, publishOptions);

        var counterLoaded = new TaskCompletionSource();
        var runOptions = new BlazorRunOptions(Configuration.Release)
        {
            BrowserPath = "counter",
            OnConsoleMessage = (type, msg) =>
            {
                if (msg.Contains("Counter.OnAfterRender"))
                    counterLoaded.TrySetResult();
            },
            Test = async (page) =>
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30_000 });

                using var requestLogClient = new BlazorWebWasmLogClient(page.Url);
                var requestLogs = await requestLogClient.GetRequestLogsAsync();
                var poisonedRequests = requestLogs.Where(log => log.WasPoisoned).ToArray();
                Assert.Equal(poisonCount, poisonedRequests.Length);
                var poisonedRequest = poisonedRequests[0];
                var clientAssemblyRequests = requestLogs
                    .Where(log => string.Equals(log.Path, poisonedRequest.Path, StringComparison.Ordinal))
                    .ToArray();

                Assert.Equal(poisonCount, clientAssemblyRequests.Length);
                Assert.All(clientAssemblyRequests, log =>
                {
                    Assert.Equal(200, log.StatusCode);
                    Assert.True(log.WasPoisoned);
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
            .WithEnvironmentVariable(PoisonClientAssemblyEnvironmentVariable, poisonCount.ToString());
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
