// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class PreloadingTests : WasmTemplateTestsBase
{
    public PreloadingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void PreloadAssets(bool isPublish, bool fingerprintDotnetJs, bool preloadAssets)
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "PreloadAssets");

        string extraMSBuildArgs = $"-p:WasmFingerprintDotnetJs={fingerprintDotnetJs.ToString().ToLower()} -p:WasmPreloadAssets={preloadAssets.ToString().ToLower()}";
        if (isPublish)
            PublishProject(info, config, new PublishOptions(ExtraMSBuildArgs: extraMSBuildArgs), wasmFingerprintDotnetJs: fingerprintDotnetJs);
        else
            BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: extraMSBuildArgs), wasmFingerprintDotnetJs: fingerprintDotnetJs);

        string? indexHtmlPath;
        if (isPublish)
        {
            indexHtmlPath = Path.Combine(
                GetBinFrameworkDir(config, forPublish: isPublish),
                "..",
                "index.html"
            );
        }
        else
        {
            string objDir = Path.Combine(GetObjDir(config), "staticwebassets", "htmlassetplaceholders", "build");
            indexHtmlPath = Directory.EnumerateFiles(objDir, "*.html").SingleOrDefault();
        }

        Assert.True(File.Exists(indexHtmlPath));
        string indexHtmlContent = File.ReadAllText(indexHtmlPath);

        Assert.Equal(preloadAssets ? 1 : 0, CountOccurrences(indexHtmlContent, "rel=\"preload\""));
        if (preloadAssets)
        {
            if (fingerprintDotnetJs)
            {
                // Expect to find fingerprinted preload
                Assert.Contains("<link href=\"_framework/dotnet", indexHtmlContent);
                Assert.DoesNotContain("<link href=\"_framework/dotnet.js\"", indexHtmlContent);
            }
            else
            {
                // Expect to find non-fingerprinted preload
                Assert.Contains("<link href=\"_framework/dotnet.js\"", indexHtmlContent);
            }
        }
    }

    public static int CountOccurrences(string source, string substring)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(substring))
            return 0;

        int count = 0;
        int index = 0;

        while ((index = source.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += substring.Length;
        }

        return count;
    }
}
