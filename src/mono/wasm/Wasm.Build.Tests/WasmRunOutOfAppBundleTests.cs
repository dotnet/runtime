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
    public async void RunOutOfAppBundle(string config, bool aot)
    {
        ProjectInfo info = CopyTestAsset(config, aot, "WasmBasicTestApp", "outofappbundle", "App");
        UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);
        bool isPublish = true;
        string binFrameworkDir = GetBinFrameworkDir(info.Configuration, isPublish);
        (string _, string output) = BuildProject(info,
            new BuildOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: binFrameworkDir,
                ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish),
                IsPublish: isPublish
        ));
        
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
            var html = $@"<!DOCTYPE html><html><body><script type=""module"" src=""{relativeMainJsPath}""></script></body></html>";
            File.WriteAllText(indexHtmlPath, html);
        }

        RunResult result = await RunForPublishWithWebServer(new(
                info.Configuration,
                TestScenario: "DotnetRun",
                CustomBundleDir: outerDir,
                ExpectedExitCode: 42)
            );
    }
}
