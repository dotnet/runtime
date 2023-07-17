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
        Id = $"{generatedProjectNamePrefix ?? assetName}_{Path.GetRandomFileName()}";
        InitBlazorWasmProjectDir(Id);

        LogPath = Path.Combine(s_buildEnv.LogRootPath, Id);
        Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, assetName), Path.Combine(_projectDir!));
    }

    protected void BuildProject(string configuration)
    {
        CommandResult result = CreateDotNetCommand().ExecuteWithCapturedOutput("build", $"-bl:{GetBinLogFilePath()}", $"-p:Configuration={configuration}");
        result.EnsureSuccessful();
    }

    protected void PublishProject(string configuration)
    {
        CommandResult result = CreateDotNetCommand().ExecuteWithCapturedOutput("publish", $"-bl:{GetBinLogFilePath()}", $"-p:Configuration={configuration}");
        result.EnsureSuccessful();
    }

    protected string GetBinLogFilePath(string suffix = null)
    {
        if (!string.IsNullOrEmpty(suffix))
            suffix = "_" + suffix;

        return Path.Combine(LogPath, $"{Id}{suffix}.binlog");
    }

    protected ToolCommand CreateDotNetCommand() => new DotNetCommand(s_buildEnv, _testOutput)
        .WithWorkingDirectory(_projectDir!)
        .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir);

    protected async Task<RunResult> RunSdkStyleApp(RunOptions options)
    {
        string runArgs = $"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files";
        string workingDirectory = Path.GetFullPath(Path.Combine(FindBlazorBinFrameworkDir(options.Configuration, forPublish: options.ForPublish), ".."));

        using var runCommand = new RunCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(workingDirectory);

        var tcs = new TaskCompletionSource<int>();

        List<string> testOutput = new();
        List<string> consoleOutput = new();
        Regex exitRegex = new Regex("WASM EXIT (?<exitCode>[0-9]+)$");

        await using var runner = new BrowserRunner(_testOutput);

        IPage page = null;

        string queryString = "?test=" + options.TestScenario;
        if (options.BrowserQueryString != null)
            queryString += "&" + string.Join("&", options.BrowserQueryString.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        page = await runner.RunAsync(runCommand, runArgs, onConsoleMessage: OnConsoleMessage, modifyBrowserUrl: url => url + queryString);

        void OnConsoleMessage(IConsoleMessage msg)
        {
            if (EnvironmentVariables.ShowBuildOutput)
                Console.WriteLine($"[{msg.Type}] {msg.Text}");

            _testOutput.WriteLine($"[{msg.Type}] {msg.Text}");
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
                options.OnConsoleMessage(msg, page);
        }

        TimeSpan timeout = TimeSpan.FromMinutes(2);
        await Task.WhenAny(tcs.Task, Task.Delay(timeout));
        if (!tcs.Task.IsCompleted)
            throw new Exception($"Timed out after {timeout.TotalSeconds}s waiting for process to exit");

        int wasmExitCode = tcs.Task.Result;
        if (options.ExpectedExitCode != null && wasmExitCode != options.ExpectedExitCode)
            throw new Exception($"Expected exit code {options.ExpectedExitCode} but got {wasmExitCode}");

        return new(wasmExitCode, testOutput, consoleOutput);
    }

    protected record RunOptions(
        string Configuration,
        string TestScenario,
        Dictionary<string, string> BrowserQueryString = null,
        bool ForPublish = false,
        Action<IConsoleMessage, IPage> OnConsoleMessage = null,
        int? ExpectedExitCode = 0
    );

    protected record RunResult(
        int ExitCode,
        IReadOnlyCollection<string> TestOutput,
        IReadOnlyCollection<string> ConsoleOutput
    );
}
