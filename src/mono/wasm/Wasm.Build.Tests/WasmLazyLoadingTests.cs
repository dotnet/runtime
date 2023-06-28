// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public class WasmLazyLoadingTests : BuildTestBase
{
    public WasmLazyLoadingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public async Task LazyLoadAssembly()
    {
        string id = $"WasmLazyLoading_{Path.GetRandomFileName()}";
        InitBlazorWasmProjectDir(id);

        string logPath = Path.Combine(s_buildEnv.LogRootPath, id);
        Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "WasmLazyLoading"), Path.Combine(_projectDir!));

        string projectFile = Path.Combine(_projectDir!, "WasmLazyLoading.csproj");
        AddItemsPropertiesToProject(projectFile);

        string publishLogPath = Path.Combine(logPath, $"{id}.binlog");
        CommandResult result = new DotNetCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(_projectDir!)
            .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
            .ExecuteWithCapturedOutput("publish", $"-bl:{publishLogPath}", $"-p:Configuration=Debug");

        result.EnsureSuccessful();


        string runArgs = $"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files";
        string workingDirectory = Path.GetFullPath(Path.Combine(FindBlazorBinFrameworkDir("Debug", forPublish: true), ".."));

        using var runCommand = new RunCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(workingDirectory);

        var tcs = new TaskCompletionSource<bool>();

        bool hasExpectedMessage = false;
        Regex exitRegex = new Regex("WASM EXIT (?<exitCode>[0-9]+)$");

        await using var runner = new BrowserRunner(_testOutput);
        var page = await runner.RunAsync(runCommand, runArgs, onConsoleMessage: OnConsoleMessage);


        void OnConsoleMessage(IConsoleMessage msg)
        {
            if (EnvironmentVariables.ShowBuildOutput)
                Console.WriteLine($"[{msg.Type}] {msg.Text}");

            _testOutput.WriteLine($"[{msg.Type}] {msg.Text}");

            if (msg.Text.Contains("FirstName"))
                hasExpectedMessage = true;

            if (exitRegex.Match(msg.Text).Success)
                tcs.SetResult(true);
        }

        TimeSpan timeout = TimeSpan.FromMinutes(2);
        await Task.WhenAny(tcs.Task, Task.Delay(timeout));
        if (!tcs.Task.IsCompleted || !tcs.Task.Result)
            throw new Exception($"Timed out after {timeout.TotalSeconds}s waiting for process to exit");

        Assert.True(hasExpectedMessage, "The lazy loading application didn't emitted expected message");
    }
}
