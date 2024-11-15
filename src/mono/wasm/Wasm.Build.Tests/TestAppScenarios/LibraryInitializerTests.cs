// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public partial class LibraryInitializerTests : WasmTemplateTestsBase
{
    public LibraryInitializerTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public async Task LoadLibraryInitializer()
    {
        string config = "Debug";        
        ProjectInfo info = CopyTestAsset(config, false, "WasmBasicTestApp", "LibraryInitializerTests_LoadLibraryInitializer", "App");
        bool isPublish = true;
        BuildProject(info,
            new BuildOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish),
                IsPublish: isPublish
        ));
        RunResult result = await RunForPublishWithWebServer(new(info.Configuration, TestScenario: "LibraryInitializerTest"));
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal("LIBRARY_INITIALIZER_TEST = 1", m)
        );
    }

    [GeneratedRegex("MONO_WASM: Failed to invoke 'onRuntimeConfigLoaded' on library initializer '../WasmBasicTestApp.[a-z0-9]+.lib.module.js': Error: Error thrown from library initializer")]
    private static partial Regex AbortStartupOnErrorRegex();

    [Fact]
    public async Task AbortStartupOnError()
    {
        string config = "Debug";        
        ProjectInfo info = CopyTestAsset(config, false, "WasmBasicTestApp", "LibraryInitializerTests_AbortStartupOnError", "App");
        bool isPublish = true;
        BuildProject(info,
            new BuildOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish),
                IsPublish: isPublish
        ));

        RunOptions options = new(
            info.Configuration,
            TestScenario: "LibraryInitializerTest",
            BrowserQueryString: new Dictionary<string, string> { ["throwError"] = "true" },
            ExpectedExitCode: 1);
        RunResult result = await RunForPublishWithWebServer(options);
        Assert.True(result.ConsoleOutput.Any(m => AbortStartupOnErrorRegex().IsMatch(m)), "The library initializer test didn't emit expected error message");
    }
}
