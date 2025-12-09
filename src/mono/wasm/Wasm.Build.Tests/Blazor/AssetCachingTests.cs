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
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Microsoft.Playwright;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class AssetCachingTests : BlazorWasmTestBase
{
    public AssetCachingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Fact]
    [Trait("Category", "no-fingerprinting")]
    public async Task BlazorApp_BasedOnFingerprinting_LoadsWasmAssetsFromCache()
    {
        ProjectInfo info = CopyTestAsset(
            Configuration.Release,
            aot: false,
            TestAsset.BlazorBasicTestApp,
            "blazor_publish");

        BlazorPublish(info, Configuration.Release);

        var startUrl = string.Empty;
        var wasmRequestRecorder = new WasmRequestRecorder();

        var runOptions = new BlazorRunOptions(Configuration.Release)
        {
            OnServerMessage = (msg) => wasmRequestRecorder.RecordIfWasmRequestFinished(msg),
            ExecuteAfterLoaded = async (_, page) => startUrl = page.Url,
            Test = async (page) =>
            {
                await WaitForCounterInteractivity(page);

                // Check resources after first load.
                // Empty DeliveryType indicates that the resource was not cached.
                var resourcesAfterFirstLoad = await GetWasmResourceEntries(page);
                Assert.NotEmpty(resourcesAfterFirstLoad);
                Assert.All(resourcesAfterFirstLoad, r => Assert.Equal(string.Empty, r.DeliveryType));

                Assert.NotEmpty(wasmRequestRecorder.ResponseCodes);
                Assert.All(wasmRequestRecorder.ResponseCodes, r => Assert.Equal(200, r.StatusCode));

                // We start recording server logs for Wasm asset requests.
                wasmRequestRecorder.ResponseCodes.Clear();

                // Perform browser navigation to cause resource reload.
                // We use the initial base URL because the test server is not configured for SPA routing.
                await page.GotoAsync(startUrl);
                await WaitForCounterInteractivity(page);

                // Check resources after second load.
                var resourcesAfterSecondLoad = await GetWasmResourceEntries(page);

                // The performance API intentionally reports response status 200 even for assets
                // retrieved from cache after validation (when browser would show status 304).
                // Therefore, we check for 200 even with fingerprinting disabled.
                Assert.NotEmpty(resourcesAfterSecondLoad);
                Assert.All(resourcesAfterSecondLoad, r => {
                    Assert.Equal("cache", r.DeliveryType);
                    Assert.Equal(200, r.ResponseStatus);
                });

                // Check server logs.
                if (EnvironmentVariables.UseFingerprinting)
                {
                    // With fingerprinting we should not see any requests for Wasm assets on second load.
                    Assert.Empty(wasmRequestRecorder.ResponseCodes);
                }
                else
                {
                    // Without fingerprinting we should see requests for Wasm assets on second load with response status 304.
                    Assert.NotEmpty(wasmRequestRecorder.ResponseCodes);
                    Assert.All(wasmRequestRecorder.ResponseCodes, r => Assert.Equal(304, r.StatusCode));
                }
            }
        };

        await RunForPublishWithDotnetServe(runOptions);
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

    /// <summary>
    /// Retrieves information about how were the page's current resources loaded using the Performance API.
    /// </summary>
    private static async Task<List<ResourceEntry>> GetWasmResourceEntries(IPage page)
    {
        var jsonData = await page.EvaluateAsync<string>("JSON.stringify(window.performance.getEntriesByType('resource'))");
        var entries = JsonSerializer.Deserialize<List<ResourceEntry>>(jsonData) ?? [];

        return entries.Where(e => e.Name.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private class ResourceEntry
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("deliveryType")]
        public required string DeliveryType { get; init; }

        [JsonPropertyName("responseStatus")]
        public required int ResponseStatus { get; init; }
    }
}

partial class WasmRequestRecorder
{
    public List<(string Name, int StatusCode)> ResponseCodes { get; } = new();

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
