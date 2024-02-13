// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit.Abstractions;
using Wasm.Build.Tests.Blazor;

namespace Wasm.Build.Tests.TestAppScenarios;

public abstract class AppTestBase : BlazorWasmTestBase
{
    protected AppTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    protected string Id { get; set; }
    protected string LogPath { get; set; }

    protected void CopyTestAsset(string assetName, string generatedProjectNamePrefix = null)
    {
        Id = $"{generatedProjectNamePrefix ?? assetName}_{GetRandomId()}";
        InitBlazorWasmProjectDir(Id);

        LogPath = Path.Combine(s_buildEnv.LogRootPath, Id);
        Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, assetName), Path.Combine(_projectDir!));

        switch(assetName)
        {
            case "WasmBasicTestApp":
                // WasmBasicTestApp consists of App + Library projects
                _projectDir = Path.Combine(_projectDir!, "App");
                break;
            case "BlazorHostedApp":
                // BlazorHostedApp consists of BlazorHosted.Client and BlazorHosted.Server projects
                _projectDir = Path.Combine(_projectDir!, "BlazorHosted.Server");
                break;
        }
    }

    protected void BuildProject(
        string configuration,
        string? binFrameworkDir = null,
        RuntimeVariant runtimeType = RuntimeVariant.SingleThreaded,
        bool assertAppBundle = true,
        params string[] extraArgs)
    {
        (CommandResult result, _) = BlazorBuild(new BlazorBuildOptions(
            Id: Id,
            Config: configuration,
            BinFrameworkDir: binFrameworkDir,
            RuntimeType: runtimeType,
            AssertAppBundle: assertAppBundle), extraArgs); 
        result.EnsureSuccessful();
    }

    protected void PublishProject(string configuration, params string[] extraArgs)
    {
        (CommandResult result, _) = BlazorPublish(new BlazorBuildOptions(Id, configuration), extraArgs);
        result.EnsureSuccessful();
    }

    protected ToolCommand CreateDotNetCommand() => new DotNetCommand(s_buildEnv, _testOutput)
        .WithWorkingDirectory(_projectDir!)
        .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir);

    protected Task<RunResult> RunSdkStyleAppForBuild(RunOptions options)
        => RunSdkStyleApp(options, BlazorRunHost.DotnetRun);
    
    protected Task<RunResult> RunSdkStyleAppForPublish(RunOptions options)
        => RunSdkStyleApp(options, BlazorRunHost.WebServer);

    private async Task<RunResult> RunSdkStyleApp(RunOptions options, BlazorRunHost host = BlazorRunHost.DotnetRun)
    {
        string queryString = "?test=" + options.TestScenario;
        if (options.BrowserQueryString != null)
            queryString += "&" + string.Join("&", options.BrowserQueryString.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        var tcs = new TaskCompletionSource<int>();
        List<string> testOutput = new();
        List<string> consoleOutput = new();
        Regex exitRegex = new Regex("WASM EXIT (?<exitCode>[0-9]+)$");

        BlazorRunOptions blazorRunOptions = new(
                CheckCounter: false,
                Config: options.Configuration,
                OnConsoleMessage: OnConsoleMessage,
                QueryString: queryString,
                ExtraArgs: options.ExtraArgs,
                Host: host);

        await BlazorRunTest(blazorRunOptions);

        void OnConsoleMessage(IConsoleMessage msg)
        {
            consoleOutput.Add(msg.Text);

            const string testOutputPrefix = "TestOutput -> ";
            if (msg.Text.StartsWith(testOutputPrefix))
                testOutput.Add(msg.Text.Substring(testOutputPrefix.Length));

            var exitMatch = exitRegex.Match(msg.Text);
            if (exitMatch.Success)
                tcs.TrySetResult(int.Parse(exitMatch.Groups["exitCode"].Value));

            if (msg.Text.StartsWith("Error: Missing test scenario"))
                throw new Exception(msg.Text);

            if (options.OnConsoleMessage != null)
                options.OnConsoleMessage(msg);
        }

        //TimeSpan timeout = TimeSpan.FromMinutes(2);
        //await Task.WhenAny(tcs.Task, Task.Delay(timeout));
        //if (!tcs.Task.IsCompleted)
            //throw new Exception($"Timed out after {timeout.TotalSeconds}s waiting for process to exit");

        int wasmExitCode = tcs.Task.Result;
        if (options.ExpectedExitCode != null && wasmExitCode != options.ExpectedExitCode)
            throw new Exception($"Expected exit code {options.ExpectedExitCode} but got {wasmExitCode}");

        return new(wasmExitCode, testOutput, consoleOutput);
    }

    protected record RunOptions(
        string Configuration,
        string TestScenario,
        Dictionary<string, string> BrowserQueryString = null,
        Action<IConsoleMessage> OnConsoleMessage = null,
        int? ExpectedExitCode = 0,
        string? ExtraArgs = null
    );

    protected record RunResult(
        int ExitCode,
        IReadOnlyCollection<string> TestOutput,
        IReadOnlyCollection<string> ConsoleOutput
    );
}
