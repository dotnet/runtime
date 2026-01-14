// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PreloadAssets(bool isPublish, bool fingerprintDotnetJs)
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "PreloadAssets");

        string extraMSBuildArgs = $"-p:WasmFingerprintDotnetJs={fingerprintDotnetJs}";
        if (isPublish)
            PublishProject(info, config, new PublishOptions(ExtraMSBuildArgs: extraMSBuildArgs), wasmFingerprintDotnetJs: fingerprintDotnetJs);
        else
            BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: extraMSBuildArgs), wasmFingerprintDotnetJs: fingerprintDotnetJs);

        string? indexHtmlPath = null;
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
