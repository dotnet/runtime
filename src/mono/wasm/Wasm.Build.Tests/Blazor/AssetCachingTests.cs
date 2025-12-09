// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
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
        var config = Configuration.Release;

        var project = CopyTestAsset(
            config,
            aot: false,
            TestAsset.BlazorWebWasm,
            "AssetCachingTest"
        );

        (string projectDir, string output) = BlazorPublish(project, config, new PublishOptions(AssertAppBundle: false));

        var startUrl = string.Empty;
        var wasmRequestRecorder = new WasmRequestRecorder();

        var runOptions = new BlazorRunOptions(Configuration.Release)
        {
            OnServerMessage = (msg) => wasmRequestRecorder.RecordIfWasmRequestFinished(msg),
            ExecuteAfterLoaded = async (_, page) => startUrl = page.Url,
            Test = async (page) =>
            {
                await WaitForCounterInteractivity(page);

                // Check server request logs after first load.
                Assert.NotEmpty(wasmRequestRecorder.ResponseCodes);
                Assert.All(wasmRequestRecorder.ResponseCodes, r => Assert.Equal(200, r.ResponseCode));

                wasmRequestRecorder.ResponseCodes.Clear();

                // Perform browser navigation to cause resource reload.
                // We use the initial base URL because the test server is not configured for SPA routing.
                await page.GotoAsync(startUrl);
                await WaitForCounterInteractivity(page);

                // Check server logs after the second load.
                if (EnvironmentVariables.UseFingerprinting)
                {
                    // With fingerprinting we should not see any requests for Wasm assets during the second load.
                    Assert.Empty(wasmRequestRecorder.ResponseCodes);
                }
                else
                {
                    // Without fingerprinting we should see validation requests for Wasm assets during the second load with response status 304.
                    Assert.NotEmpty(wasmRequestRecorder.ResponseCodes);
                    Assert.All(wasmRequestRecorder.ResponseCodes, r => Assert.Equal(304, r.ResponseCode));
                }
            }
        };

        var buildPaths = GetBuildPaths(Configuration.Release, forPublish: true, projectDir: projectDir);
        var publishedAppPath = Path.Combine(buildPaths.BinDir, "publish");
        var publishedAppDllPath = Path.Combine(publishedAppPath, project.ProjectName + ".dll");
        using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput).WithWorkingDirectory(publishedAppPath);
        var result = await BrowserRun(cmd, $"exec \"{publishedAppDllPath}\"", runOptions);
    }

    private static async Task WaitForCounterInteractivity(IPage page)
    {
        await page.Locator("text=Counter").ClickAsync();
        var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
        Assert.Equal("Current count: 0", txt);

        await page.Locator("text=\"Click me\"").ClickAsync();
        txt = await page.Locator("p[role='status']").InnerHTMLAsync();
        Assert.Equal("Current count: 1", txt);
    }
}

partial class WasmRequestRecorder
{
    public List<(string Name, int ResponseCode)> ResponseCodes { get; } = new();

    [GeneratedRegex(@"Request finished HTTP/\d\.\d GET http://[^/]+/(?<name>[^\s]+\.wasm)\s+-\s+(?<code>\d+)")]
    private static partial Regex LogRegex();

    public void RecordIfWasmRequestFinished(string message)
    {
		var match = LogRegex().Match(message);

		if (match.Success)
		{
			var name = match.Groups["name"].Value;
			var code = int.Parse(match.Groups["code"].Value);
			ResponseCodes.Add((name, code));
		}
    }
}
