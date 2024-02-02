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

namespace Wasm.Build.Templates.Tests;

public class InterpPgoTests : WasmTemplateTestBase
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

        string id = $"browser_{config}_{GetRandomId()}";
        _testOutput.WriteLine("/// Creating project");
        string projectFile = CreateWasmTemplateProject(id, "wasmbrowser");

        _testOutput.WriteLine("/// Updating JS");
        UpdateBrowserMainJs((js) => {
            // We need to capture INTERNAL so we can explicitly save the PGO table
            js = js.Replace(
                "const { setModuleImports, getAssemblyExports, getConfig, runMain } = await dotnet",
                "const { setModuleImports, getAssemblyExports, getConfig, runMain, INTERNAL } = await dotnet"
            );
            // Enable interpreter PGO + interpreter PGO logging + console output capturing
            js = js.Replace(
                ".create()",
                ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().withExitOnUnhandledError().withRuntimeOptions(['--interp-pgo-logging']).withInterpreterPgo(true).create()"
            );
            js = js.Replace("runMain()", "dotnet.run()");
            // Call Greeting in a loop to exercise enough code to cause something to tier,
            //  then call INTERNAL.interp_pgo_save_data() to save the interp PGO table
            js = js.Replace(
                "const text = exports.MyClass.Greeting();",
                "let text = '';\n" +
                $"for (let i = 0; i < {iterationCount}; i++) {{ text = exports.MyClass.Greeting(); }};\n" +
                "await INTERNAL.interp_pgo_save_data();"
            );
            return js;
        }, DefaultTargetFramework);

        _testOutput.WriteLine("/// Building");

        new DotNetCommand(s_buildEnv, _testOutput)
                .WithWorkingDirectory(_projectDir!)
                .Execute($"build -c {config} -bl:{Path.Combine(s_buildEnv.LogRootPath, $"{id}.binlog")}")
                .EnsureSuccessful();

        _testOutput.WriteLine("/// Starting server");

        // Create a single browser instance and single context to host all our pages.
        // If we don't do this, each page will have its own unique cache and the table won't be loaded.
        using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!);
        await using var runner = new BrowserRunner(_testOutput);
        var url = await runner.StartServerAndGetUrlAsync(runCommand, $"run --no-silent -c {config} --no-build --project \"{projectFile}\" --forward-console");
        IBrowser browser = await runner.SpawnBrowserAsync(url);
        IBrowserContext context = await browser.NewContextAsync();

        string output;
        {
            _testOutput.WriteLine("/// First run");
            var page = await runner.RunAsync(context, url);
            await runner.WaitForExitMessageAsync(TimeSpan.FromSeconds(30));
            lock (runner.OutputLines)
                output = string.Join(Environment.NewLine, runner.OutputLines);

            Assert.Contains("Hello, Browser!", output);
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

            Assert.Contains("Hello, Browser!", output);
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
