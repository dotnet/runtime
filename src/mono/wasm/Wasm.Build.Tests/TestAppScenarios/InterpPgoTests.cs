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
    // would get added to the PGO table instead of just hot ones.
    [InlineData("Release")]
    public async Task FirstRunGeneratesTableAndSecondRunLoadsIt(string config)
    {
        // We need to invoke Greeting enough times to cause BCL code to tier so we can exercise interpreter PGO
        // Invoking it too many times makes the test meaningfully slower.
        const int iterationCount = 70;

        string id = $"browser_{config}_{GetRandomId()}";
        
        _testOutput.WriteLine("/// Creating project");
        CopyTestAsset("WasmBasicTestApp", "InterpPgoTest", "App");
        BuildProject(config);
        
        var result = await RunTest(config, iterationCount);
        string output;
        {
            _testOutput.WriteLine("/// First run");
            output = string.Join(Environment.NewLine, result.ConsoleOutput);

            Assert.Contains("Hello, World! Greetings from", output);
            // Verify that no PGO table was located in cache
            Assert.Contains("Failed to load interp_pgo table", output);
            // Verify that the table was saved after the app ran
            Assert.Contains("Saved interp_pgo table", output);
            // Verify that a specific method was tiered by the Greeting calls and recorded by PGO
            Assert.Contains("added System.Runtime.CompilerServices.Unsafe:Add<byte> (byte&,int) to table", output);
        }

        result = await RunTest(config, iterationCount);
        {
            _testOutput.WriteLine("/// Second run");
            output = string.Join(Environment.NewLine, result.ConsoleOutput);

            Assert.Contains("Hello, World! Greetings from", output);
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
    }

    private Task<RunResult> RunTest(string config, int iterationCount) => RunSdkStyleAppForBuild(new(Configuration: config, TestScenario: "InterpPgoTest", BrowserQueryString: new() { ["iterations"] = iterationCount.ToString() }));
}
