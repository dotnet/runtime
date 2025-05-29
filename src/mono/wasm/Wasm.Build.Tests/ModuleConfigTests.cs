// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

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
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, $"ModuleConfigTests_DownloadProgressFinishes_{failAssemblyDownload}");
        PublishProject(info, config);

        var result = await RunForPublishWithWebServer(new BrowserRunOptions(
            Configuration: config,
            TestScenario: "DownloadResourceProgressTest",
            BrowserQueryString: new NameValueCollection { {"failAssemblyDownload", failAssemblyDownload.ToString().ToLowerInvariant() } }
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
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "ModuleConfigTests_OutErrOverrideWorks");
        PublishProject(info, config);

        var result = await RunForPublishWithWebServer(new BrowserRunOptions(
            Configuration: Configuration.Debug,
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
    [InlineData(Configuration.Release, true)]
    [InlineData(Configuration.Release, false)]
    public async Task OverrideBootConfigName(Configuration config, bool isPublish)
    {
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, $"OverrideBootConfigName_{isPublish}");

        if (isPublish)
            PublishProject(info, config, new PublishOptions(BootConfigFileName: "boot.json", UseCache: false));
        else
            BuildProject(info, config, new BuildOptions(BootConfigFileName: "boot.json", UseCache: false));

        var runOptions = new BrowserRunOptions(
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
