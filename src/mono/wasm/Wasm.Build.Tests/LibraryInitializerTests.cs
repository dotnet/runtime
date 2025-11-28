// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests;

public partial class LibraryInitializerTests : WasmTemplateTestsBase
{
    public LibraryInitializerTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact, TestCategory("bundler-friendly")]
    public async Task LoadLibraryInitializer()
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "LibraryInitializerTests_LoadLibraryInitializer");
        PublishProject(info, config);
        RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "LibraryInitializerTest"));
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal("LIBRARY_INITIALIZER_TEST = 1", m)
        );
    }

    [GeneratedRegex("MONO_WASM: Failed to invoke 'onRuntimeConfigLoaded' on library initializer '../WasmBasicTestApp.[a-z0-9]+.lib.module.js': Error: Error thrown from library initializer")]
    private static partial Regex AbortStartupOnErrorRegex();

    [Fact, TestCategory("bundler-friendly")]
    public async Task AbortStartupOnError()
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "LibraryInitializerTests_AbortStartupOnError");
        PublishProject(info, config);

        BrowserRunOptions options = new(
            config,
            TestScenario: "LibraryInitializerTest",
            BrowserQueryString: new NameValueCollection { {"throwError", "true" } },
            ExpectedExitCode: 1);
        RunResult result = await RunForPublishWithWebServer(options);
        Assert.True(result.ConsoleOutput.Any(m => AbortStartupOnErrorRegex().IsMatch(m)), "The library initializer test didn't emit expected error message");
    }
}
