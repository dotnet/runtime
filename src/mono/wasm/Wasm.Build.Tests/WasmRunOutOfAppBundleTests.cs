// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class WasmRunOutOfAppBundleTests : WasmTemplateTestsBase
{
    public WasmRunOutOfAppBundleTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : base(output, buildContext)
    {}

    [Theory]
    [BuildAndRun]
    public async void RunOutOfAppBundle(Configuration config, bool aot)
    {
        ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "outofappbundle");
        UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);
        (string _, string output) = PublishProject(info, config, new PublishOptions(AOT: aot));
        
        string binFrameworkDir = GetBinFrameworkDir(config, forPublish: true);
        string appBundleDir = Path.Combine(binFrameworkDir, "..");
        string outerDir = Path.GetFullPath(Path.Combine(appBundleDir, ".."));        
        string indexHtmlPath = Path.Combine(appBundleDir, "index.html");
        // Delete the original one, so we don't use that by accident
        if (File.Exists(indexHtmlPath))
            File.Delete(indexHtmlPath);
        
        indexHtmlPath = Path.Combine(outerDir, "index.html");
        string relativeMainJsPath = "./wwwroot/main.js";
        if (!File.Exists(indexHtmlPath))
        {
            var html = $@"<!DOCTYPE html><html><head></head><body><script type=""module"" src=""{relativeMainJsPath}""></script></body></html>";
            File.WriteAllText(indexHtmlPath, html);
        }

        UpdateBootJsInHtmlFile(indexHtmlPath);

        RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(
                config,
                TestScenario: "DotnetRun",
                CustomBundleDir: outerDir,
                ExpectedExitCode: 42)
            );
    }
}
