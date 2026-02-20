// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class AssetCachingTests : BlazorWasmTestBase
{
    public AssetCachingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
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
                await counterLoaded.Task;

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
                await counterLoaded.Task;

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
