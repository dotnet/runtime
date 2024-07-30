// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using Microsoft.Playwright;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class InterpPgoTests : AppTestBase
{
    public InterpPgoTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    // Interpreter PGO is not meaningful to enable in debug builds - tiering is inactive there so all methods
    //  would get added to the PGO table instead of just hot ones.
    [InlineData("Release")]
    public async Task FirstRunGeneratesTableAndSecondRunLoadsIt(string config)
    {
        // We need to invoke Greeting enough times to cause BCL code to tier so we can exercise interpreter PGO
        // Invoking it too many times makes the test meaningfully slower.
        const int iterationCount = 70;

        _testOutput.WriteLine("/// Creating project");
        CopyTestAsset("WasmBasicTestApp", "InterpPgoTest", "App");

        _testOutput.WriteLine("/// Building");
        BuildProject(config, extraArgs: "-p:WasmDebugLevel=0");

        _testOutput.WriteLine("/// Starting server");

        // Create a single browser instance and single context to host all our pages.
        // If we don't do this, each page will have its own unique cache and the table won't be loaded.
        using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!);
        await using var runner = new BrowserRunner(_testOutput);
        var url = await runner.StartServerAndGetUrlAsync(runCommand, $"run --no-silent -c {config} --no-build --project \"{_projectDir!}\" --forward-console");
        url = $"{url}?test=InterpPgoTest&iterationCount={iterationCount}";

        _testOutput.WriteLine($"/// Spawning browser at URL {url}");

        IBrowser browser = await runner.SpawnBrowserAsync(url);
        IBrowserContext context = await browser.NewContextAsync();

        string output;
        {
            _testOutput.WriteLine("/// First run");
            var page = await runner.RunAsync(context, url);
            await runner.WaitForExitMessageAsync(TimeSpan.FromSeconds(6 * 30));
            lock (runner.OutputLines)
                output = string.Join(Environment.NewLine, runner.OutputLines);

            Assert.Contains("Hello, World!", output);
            // Verify that no PGO table was located in cache
            Assert.Contains("Failed to load interp_pgo table", output);
            // Verify that the table was saved after the app ran
            Assert.Contains("Saved interp_pgo table", output);
            // Verify that a specific method was tiered by the Greeting calls and recorded by PGO
            Assert.Contains("added System.Runtime.CompilerServices.Unsafe:Add<byte> (byte&,int) to table", output);
        }

        {
            _testOutput.WriteLine("/// Second run");
            // Clear the shared output lines buffer so it's empty for the next run.
            lock (runner.OutputLines)
                runner.OutputLines.Clear();
            // resetExitedState is necessary for WaitForExitMessageAsync to work correctly
            var page = await runner.RunAsync(context, url, resetExitedState: true);
            await runner.WaitForExitMessageAsync(TimeSpan.FromSeconds(30));
            lock (runner.OutputLines)
                output = string.Join(Environment.NewLine, runner.OutputLines);

            Assert.Contains("Hello, World!", output);
            // Verify that table data was loaded from cache
            // if this breaks, it could be caused by change in config which affects the config hash and the cache storage hash key
            Assert.Contains(" bytes of interp_pgo data (table size == ", output);
            // Verify that the table was saved after the app ran
            Assert.Contains("Saved interp_pgo table", output);
            // Verify that method(s) were found in the table and eagerly tiered
            Assert.Contains("because it was in the interp_pgo table", output);
            // Verify that a specific method was tiered by the Greeting calls and recorded by PGO
            Assert.Contains("added System.Runtime.CompilerServices.Unsafe:Add<byte> (byte&,int) to table", output);
        }

        _testOutput.WriteLine("/// Done");

        (context as IDisposable)?.Dispose();
        (browser as IDisposable)?.Dispose();
    }
}
