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

public class WasmAppBuilderDebugLevelTests : DebugLevelTestsBase
{
    public WasmAppBuilderDebugLevelTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    protected override void SetupProject(string projectId)
    {
        Id = $"{projectId}_{GetRandomId()}";
        string projectfile = CreateWasmTemplateProject(Id, "wasmconsole", extraArgs: "-f net8.0");
        string projectDir = Path.GetDirectoryName(projectfile)!;
        string mainJs = Path.Combine(projectDir, "main.mjs");
        string mainJsContent = File.ReadAllText(mainJs);
        mainJsContent = mainJsContent
            .Replace("await dotnet.run()", "console.log('TestOutput -> WasmDebugLevel: ' + config.debugLevel); exit(0)");
        File.WriteAllText(mainJs, mainJsContent);
    }

    protected override Task<RunResult> RunForBuild(string configuration)
    {
        CommandResult res = new RunCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(_projectDir!)
            .ExecuteWithCapturedOutput($"run --no-silent --no-build -c {configuration}");

        return Task.FromResult(ProcessRunOutput(res));
    }

    private RunResult ProcessRunOutput(CommandResult res)
    {
        var output = res.Output.Split(Environment.NewLine);
        _testOutput.WriteLine($"DEBUG: parsed lines '{String.Join(", ", output)}'");

        var prefix = "[] TestOutput -> ";
        var testOutput = output
            .Where(l => l.StartsWith(prefix))
            .Select(l => l.Substring(prefix.Length))
            .ToArray();

        _testOutput.WriteLine($"DEBUG: testOutput '{String.Join(", ", testOutput)}'");
        return new RunResult(res.ExitCode, testOutput, output);
    }

    protected override Task<RunResult> RunForPublish(string configuration)
    {
        // WasmAppBuilder does publish to the same folder as build (it overrides the output), 
        // and thus using dotnet run work correctly for publish as well.
        CommandResult res = new RunCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(_projectDir!)
            .ExecuteWithCapturedOutput($"run --no-silent --no-build -c {configuration}");

        return Task.FromResult(ProcessRunOutput(res));
    }
}
