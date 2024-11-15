// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class ModuleConfigTests : WasmTemplateTestsBase
{
    public ModuleConfigTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DownloadProgressFinishes(bool failAssemblyDownload)
    {
        string config = "Debug";
        ProjectInfo info = CopyTestAsset(config, false, "WasmBasicTestApp", $"ModuleConfigTests_DownloadProgressFinishes_{failAssemblyDownload}", "App");
        bool isPublish = true;
        BuildProject(info,
            new BuildOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish),
                IsPublish: isPublish
        ));

        var result = await RunForPublishWithWebServer(new(
            Configuration: info.Configuration,
            TestScenario: "DownloadResourceProgressTest",
            BrowserQueryString: new Dictionary<string, string> { ["failAssemblyDownload"] = failAssemblyDownload.ToString().ToLowerInvariant() }
        ));
        Assert.True(
            result.TestOutput.Any(m => m.Contains("DownloadResourceProgress: Finished")),
            "The download progress test didn't emit expected error message"
        );
        Assert.True(
            result.ConsoleOutput.Any(m => m.Contains("Retrying download")) == failAssemblyDownload,
            failAssemblyDownload
                ? "The download progress test didn't emit expected message about retrying download"
                : "The download progress test did emit unexpected message about retrying download"
        );
        Assert.False(
            result.ConsoleOutput.Any(m => m.Contains("Retrying download (2)")),
            "The download progress test did emit unexpected message about second download retry"
        );
        Assert.True(
            result.TestOutput.Any(m => m.Contains("Throw error instead of downloading resource") == failAssemblyDownload),
            failAssemblyDownload
                ? "The download progress test didn't emit expected message about failing download"
                : "The download progress test did emit unexpected message about failing download"
        );
    }

    [Fact]
    public async Task OutErrOverrideWorks()
    {
        string config = "Debug";
        ProjectInfo info = CopyTestAsset(config, false, "WasmBasicTestApp", "ModuleConfigTests_OutErrOverrideWorks", "App");
        bool isPublish = true;
        BuildProject(info,
            new BuildOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish),
                IsPublish: isPublish
        ));

        var result = await RunForPublishWithWebServer(new(
            Configuration: "Debug",
            TestScenario: "OutErrOverrideWorks"
        ));
        Assert.True(
            result.ConsoleOutput.Any(m => m.Contains("Emscripten out override works!")),
            "Emscripten out override doesn't work"
        );
        Assert.True(
            result.ConsoleOutput.Any(m => m.Contains("Emscripten err override works!")),
            "Emscripten err override doesn't work"
        );
    }

    [Theory]
    [InlineData("Release", true)]
    [InlineData("Release", false)]
    public async Task OverrideBootConfigName(string config, bool isPublish)
    {
        ProjectInfo info = CopyTestAsset(config, false, "WasmBasicTestApp", "OverrideBootConfigName", "App");
        BuildProject(info,
            new BuildOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish),
                IsPublish: isPublish
        ));

        string[] extraArgs = ["-p:WasmBootConfigFileName=boot.json"];
        BuildProject(info,
            new BuildOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish),
                IsPublish: isPublish,
                BootConfigFileName: "boot.json",
                UseCache: false
            ),
            extraArgs: extraArgs
        );

        var runOptions = new RunOptions(
            Configuration: config,
            TestScenario: "OverrideBootConfigName"
        );
        var result = await (isPublish
            ? RunForPublishWithWebServer(runOptions)
            : RunForBuildWithDotnetRun(runOptions)
        );

        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal("ConfigSrc: boot.json", m),
            m => Assert.Equal("Managed code has run", m)
        );
    }
}
