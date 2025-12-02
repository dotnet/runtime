// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    public async Task BlazorApp_BasedOnFingerprinting_LoadsWasmAssetsFromCache()
    {
        ProjectInfo info = CopyTestAsset(
            Configuration.Release,
            aot: false,
            TestAsset.BlazorBasicTestApp,
            "blazor_publish");

        BlazorPublish(info, Configuration.Release);

        var startUrl = string.Empty;
        var runOptions = new BlazorRunOptions(Configuration.Release)
        {
            ExecuteAfterLoaded = async (_, page) => startUrl = page.Url,
            Test = async (page) =>
            {
                // Check resources after first load.
                // Empty DeliveryType indicates that the resource was not cached.
                var resourcesAfterFirstLoad = await GetWasmResourceEntries(page);
                Assert.All(resourcesAfterFirstLoad, r => Assert.Equal(string.Empty, r.DeliveryType));

                // Perform browser navigation to cause resource reload.
                // We use the initial base URL because the test server is not configured for SPA routing.
                await page.GotoAsync(startUrl);

                // Check resources after second load.
                // ResponseStatus 200 indicates loading from cache without validation request.
                var resourcesAfterSecondLoad = await GetWasmResourceEntries(page);

                if (EnvironmentVariables.UseFingerprinting)
                {
                    Assert.All(resourcesAfterSecondLoad, r => {
                        Assert.Equal("cache", r.DeliveryType);
                        Assert.Equal(200, r.ResponseStatus);
                    });
                }
                else
                {
                    Assert.All(resourcesAfterSecondLoad, r => {
                        Assert.Equal("cache", r.DeliveryType);
                        Assert.Equal(304, r.ResponseStatus);
                    });
                }
            }
        };

        await RunForPublishWithWebServer(runOptions);
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
