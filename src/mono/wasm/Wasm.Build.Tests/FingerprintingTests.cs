// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests;

public class FingerprintingTests : WasmTemplateTestsBase
{
    public FingerprintingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public async Task TestOverrideHtmlAssetPlaceholdersWithFingerprinting()
    {
        var config = Configuration.Release;
        string extraProperties = "<OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>";
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "OverrideHtmlAssetPlaceholders", extraProperties: extraProperties);
        UpdateFile(Path.Combine("wwwroot", "index.html"), new Dictionary<string, string> {
            { """<script type='module' src="./main.js"></script>""", """<script type="importmap"></script> <script type='module' src="./main.js"></script>""" }
        });
        BuildProject(info, config, wasmFingerprintDotnetJs: true);
        BrowserRunOptions runOptions = new(config, TestScenario: "DotnetRun");
        await RunForBuildWithDotnetRun(runOptions);
        
        PublishProject(info, config, new PublishOptions(UseCache: false), wasmFingerprintDotnetJs: true);
        await RunForPublishWithWebServer(runOptions);
    }
}
