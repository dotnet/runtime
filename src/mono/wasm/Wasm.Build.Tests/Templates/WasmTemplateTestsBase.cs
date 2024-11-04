// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public class WasmTemplateTestsBase : BuildTestBase
{
    private readonly WasmSdkBasedProjectProvider _provider;
    protected const string DefaultRuntimeAssetsRelativePath = "./_framework/";
    public WasmTemplateTestsBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext, ProjectProviderBase? provider = null)
        : base(provider ?? new WasmSdkBasedProjectProvider(output, DefaultTargetFramework), output, buildContext)
    {
        _provider = GetProvider<WasmSdkBasedProjectProvider>();
    }

    private Dictionary<string, string> browserProgramReplacements = new Dictionary<string, string>
        {
            { "while(true)", $"int i = 0;{Environment.NewLine}while(i++ < 0)" },  // the test has to be fast, skip the loop
            { "partial class StopwatchSample", $"return 42;{Environment.NewLine}partial class StopwatchSample" }
        };

    private string GetProjectName(string idPrefix, string config, bool aot, bool appendUnicodeToPath, bool avoidAotLongPathIssue = false) =>
        avoidAotLongPathIssue ? // https://github.com/dotnet/runtime/issues/103625
            $"{GetRandomId()}" :
            appendUnicodeToPath ?
                $"{idPrefix}_{config}_{aot}_{GetRandomId()}_{s_unicodeChars}" :
                $"{idPrefix}_{config}_{aot}_{GetRandomId()}";

    private string InitProjectLocation(string idPrefix, string config, bool aot, bool appendUnicodeToPath, bool avoidAotLongPathIssue = false)
    {
        string projectName = GetProjectName(idPrefix, config, aot, appendUnicodeToPath, avoidAotLongPathIssue);
        InitPaths(projectName);
        InitProjectDir(_projectDir, addNuGetSourceForLocalPackages: true);
        return projectName;
    }

    public ProjectInfo CreateWasmTemplateProject(
        Template template,
        string config,
        bool aot,
        string idPrefix = "wbt",
        bool appendUnicodeToPath = true,
        string extraArgs = "",
        bool runAnalyzers = true,
        bool addFrameworkArg = false,
        string extraProperties = "",
        string extraItems = "",
        string insertAtEnd = "")
    {
        string projectName = InitProjectLocation(idPrefix, config, aot, appendUnicodeToPath);

        if (addFrameworkArg)
            extraArgs += $" -f {DefaultTargetFramework}";

        using DotNetCommand cmd = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false);
        CommandResult result = cmd.WithWorkingDirectory(_projectDir!)
            .ExecuteWithCapturedOutput($"new {template.ToString().ToLower()} {extraArgs}")
            .EnsureSuccessful();

        string projectFilePath = Path.Combine(_projectDir!, $"{projectName}.csproj");
        UpdateProjectFile(projectFilePath, aot, runAnalyzers, extraProperties, extraItems, insertAtEnd);
        return new ProjectInfo(config, aot, projectName, projectFilePath);
    }

    protected ProjectInfo CopyTestAsset(
        string config,
        bool aot,
        string assetDirName,
        string idPrefix,
        string projectDirRelativeToAssetDir = "",
        bool appendUnicodeToPath = true,
        bool runAnalyzers = true,
        string extraProperties = "",
        string extraItems = "",
        string insertAtEnd = "")
    {
        InitProjectLocation(idPrefix, config, aot, appendUnicodeToPath, avoidAotLongPathIssue: s_isWindows && aot);
        Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, assetDirName), Path.Combine(_projectDir!));
        if (!string.IsNullOrEmpty(projectDirRelativeToAssetDir))
        {
            _projectDir = Path.Combine(_projectDir!, projectDirRelativeToAssetDir);
        }
        string projectFilePath = Path.Combine(_projectDir!, $"{assetDirName}.csproj");
        UpdateProjectFile(projectFilePath, aot, runAnalyzers, extraProperties, extraItems, insertAtEnd);
        return new ProjectInfo(config, aot, assetDirName, projectFilePath);
    }

    private void UpdateProjectFile(string projectFilePath, bool aot, bool runAnalyzers, string extraProperties, string extraItems, string insertAtEnd)
    {
        if (aot)
        {
            extraProperties += $"\n<RunAOTCompilation>true</RunAOTCompilation>";
            extraProperties += $"\n<EmccVerbose>{s_isWindows}</EmccVerbose>";
        }
        extraProperties += "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>";
        if (runAnalyzers)
            extraProperties += "<RunAnalyzers>true</RunAnalyzers>";
        AddItemsPropertiesToProject(projectFilePath, extraProperties, extraItems, insertAtEnd);
    }


    public (string projectDir, string buildOutput) BuildTemplateProject(
        ProjectInfo projectInfo,
        BuildProjectOptions buildOptions,
        params string[] extraArgs)
    {
        if (buildOptions.ExtraBuildEnvironmentVariables is null)
            buildOptions = buildOptions with { ExtraBuildEnvironmentVariables = new Dictionary<string, string>() };

        // TODO: reenable this when the SDK supports targetting net10.0
        //buildOptions.ExtraBuildEnvironmentVariables["TreatPreviousAsCurrent"] = "false";

        (CommandResult res, string logFilePath) = BuildProjectWithoutAssert(buildOptions, extraArgs);

        if (buildOptions.UseCache)
            _buildContext.CacheBuild(projectInfo, new BuildProduct(_projectDir!, logFilePath, true, res.Output));

        if (!buildOptions.ExpectSuccess)
        {
            res.EnsureFailed();
            return (_projectDir!, res.Output);
        }

        if (string.IsNullOrEmpty(buildOptions.BinFrameworkDir))
        {
            buildOptions = buildOptions with { BinFrameworkDir = GetBinFrameworkDir(buildOptions.Configuration, buildOptions.IsPublish) };
        }

        if (buildOptions.AssertAppBundle)
        {
            _provider.AssertWasmSdkBundle(buildOptions, res.Output);
        }
        return (_projectDir!, res.Output);
    }

    private string StringReplaceWithAssert(string oldContent, string oldValue, string newValue)
    {
        string newContent = oldContent.Replace(oldValue, newValue);
        if (oldValue != newValue && oldContent == newContent)
            throw new XunitException($"Replacing '{oldValue}' with '{newValue}' did not change the content '{oldContent}'");

        return newContent;
    }

    protected void UpdateBrowserProgramFile() =>
        UpdateFile("Program.cs", browserProgramReplacements);

    protected void UpdateFile(string pathRelativeToProjectDir, Dictionary<string, string> replacements)
    {
        var path = Path.Combine(_projectDir!, pathRelativeToProjectDir);
        string text = File.ReadAllText(path);
        foreach (var replacement in replacements)
        {
            text = StringReplaceWithAssert(text, replacement.Key, replacement.Value);
        }
        File.WriteAllText(path, text);
    }

    protected void UpdateFile(string pathRelativeToProjectDir, string newContent)
    {
        var updatedFilePath = Path.Combine(_projectDir!, pathRelativeToProjectDir);
        File.WriteAllText(updatedFilePath, newContent);
    }

    protected void ReplaceFile(string pathRelativeToProjectDir, string pathWithNewContent)
    {
        string newContent = File.ReadAllText(pathWithNewContent);
        UpdateFile(pathRelativeToProjectDir, newContent);
    }

    protected void RemoveContentsFromProjectFile(string pathRelativeToProjectDir, string afterMarker, string beforeMarker)
    {
        var path = Path.Combine(_projectDir!, pathRelativeToProjectDir);
        string text = File.ReadAllText(path);
        int start = text.IndexOf(afterMarker);
        int end = text.IndexOf(beforeMarker, start);
        if (start == -1 || end == -1)
            throw new XunitException($"Start or end marker not found in '{path}'");
        start += afterMarker.Length;
        text = text.Remove(start, end - start);
        // separate the markers with a new line
        text = text.Insert(start, "\n");
        File.WriteAllText(path, text);
    }

    protected void UpdateBrowserMainJs(string targetFramework = DefaultTargetFramework, string runtimeAssetsRelativePath = DefaultRuntimeAssetsRelativePath)
    {
        string mainJsPath = Path.Combine(_projectDir!, "wwwroot", "main.js");
        string mainJsContent = File.ReadAllText(mainJsPath);

        string updatedMainJsContent = StringReplaceWithAssert(
            mainJsContent,
            ".create()",
            (targetFramework == "net8.0" || targetFramework == "net9.0")
                    ? ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().withExitOnUnhandledError().create()"
                    : ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().create()"
            );

        // dotnet.run() is already used in <= net8.0
        if (targetFramework != "net8.0")
            updatedMainJsContent = StringReplaceWithAssert(updatedMainJsContent, "runMain()", "dotnet.run()");

        updatedMainJsContent = StringReplaceWithAssert(updatedMainJsContent, "from './_framework/dotnet.js'", $"from '{runtimeAssetsRelativePath}dotnet.js'");


        File.WriteAllText(mainJsPath, updatedMainJsContent);
    }

    // Keeping these methods with explicit Build/Publish in the name
    // so in the test code it is evident which is being run!
    public async Task<RunResult> RunForBuildWithDotnetRun(RunOptions runOptions)
        => await BrowserRun(runOptions with { Host = RunHost.DotnetRun });

    public async Task<RunResult> RunForPublishWithWebServer(RunOptions runOptions)
        => await BrowserRun(runOptions with { Host = RunHost.WebServer });

    private async Task<RunResult> BrowserRun(RunOptions runOptions) => runOptions.Host switch
    {
        RunHost.DotnetRun =>
                await BrowserRunTest($"run -c {runOptions.Configuration} --no-build", _projectDir!, runOptions),

        RunHost.WebServer =>
                await BrowserRunTest($"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files",
                    string.IsNullOrEmpty(runOptions.CustomBundleDir) ?
                        Path.GetFullPath(Path.Combine(GetBinFrameworkDir(runOptions.Configuration, forPublish: true), "..")) :
                        runOptions.CustomBundleDir,
                     runOptions),

        _ => throw new NotImplementedException(runOptions.Host.ToString())
    };

    protected async Task<RunResult> BrowserRunTest(string runArgs,
                                    string workingDirectory,
                                    RunOptions runOptions)
    {
        if (!string.IsNullOrEmpty(runOptions.ExtraArgs))
            runArgs += $" {runOptions.ExtraArgs}";

        runOptions.ServerEnvironment?.ToList().ForEach(
            kv => s_buildEnv.EnvVars[kv.Key] = kv.Value);

        using RunCommand runCommand = new RunCommand(s_buildEnv, _testOutput);
        ToolCommand cmd = runCommand.WithWorkingDirectory(workingDirectory);

        var query = runOptions.BrowserQueryString ?? new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(runOptions.TestScenario))
            query.Add("test", runOptions.TestScenario);
        var queryString = query.Any() ? "?" + string.Join("&", query.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "";

        List<string> testOutput = new();
        List<string> consoleOutput = new();
        List<string> serverOutput = new();
        await using var runner = new BrowserRunner(_testOutput);
        var page = await runner.RunAsync(
            cmd,
            runArgs,
            locale: runOptions.Locale,
            onConsoleMessage: OnConsoleMessage,
            onServerMessage: OnServerMessage,
            onError: OnErrorMessage,
            modifyBrowserUrl: browserUrl => new Uri(new Uri(browserUrl), runOptions.BrowserPath + queryString).ToString());

        _testOutput.WriteLine("Waiting for page to load");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new () { Timeout = 1 * 60 * 1000 });

        if (runOptions.ExecuteAfterLoaded is not null)
        {
            await runOptions.ExecuteAfterLoaded(runOptions, page);
        }

        if (runOptions.Test is not null)
            await runOptions.Test(page);

        _testOutput.WriteLine($"Waiting for additional 10secs to see if any errors are reported");
        int exitCode = await runner.WaitForExitMessageAsync(TimeSpan.FromSeconds(10));
        if (runOptions.ExpectedExitCode is not null && exitCode != runOptions.ExpectedExitCode)
            throw new Exception($"Expected exit code {runOptions.ExpectedExitCode} but got {exitCode}.\nconsoleOutput={string.Join("\n", consoleOutput)}");

        return new(exitCode, testOutput, consoleOutput, serverOutput);

        void OnConsoleMessage(string type, string msg)
        {
            _testOutput.WriteLine($"[{type}] {msg}");
            consoleOutput.Add(msg);
            OnTestOutput(msg);

            runOptions.OnConsoleMessage?.Invoke(type, msg);

            if (runOptions.DetectRuntimeFailures)
            {
                if (msg.Contains("[MONO] * Assertion") || msg.Contains("Error: [MONO] "))
                    throw new XunitException($"Detected a runtime failure at line: {msg}");
            }
        }

        void OnServerMessage(string msg)
        {
            serverOutput.Add(msg);
            OnTestOutput(msg);

            if (runOptions.OnServerMessage != null)
                runOptions.OnServerMessage(msg);
        }

        void OnTestOutput(string msg)
        {
            const string testOutputPrefix = "TestOutput -> ";
            if (msg.StartsWith(testOutputPrefix))
                testOutput.Add(msg.Substring(testOutputPrefix.Length));
        }

        void OnErrorMessage(string msg)
        {
            _testOutput.WriteLine($"[ERROR] {msg}");
            runOptions.OnErrorMessage?.Invoke(msg);
        }
    }

    public string GetBinFrameworkDir(string config, bool forPublish, string framework = DefaultTargetFramework, string? projectDir = null) =>
        _provider.GetBinFrameworkDir(config, forPublish, framework, projectDir);

    public BuildPaths GetBuildPaths(ProjectInfo info, bool forPublish) =>
        _provider.GetBuildPaths(info, forPublish);

    public IDictionary<string, (string fullPath, bool unchanged)> GetFilesTable(ProjectInfo info, BuildPaths paths, bool unchanged) =>
        _provider.GetFilesTable(info, paths, unchanged);

    public IDictionary<string, FileStat> StatFiles(IDictionary<string, (string fullPath, bool unchanged)> fullpaths) =>
        _provider.StatFiles(fullpaths);

    public void CompareStat(IDictionary<string, FileStat> oldStat, IDictionary<string, FileStat> newStat, IDictionary<string, (string fullPath, bool unchanged)> expected) =>
        _provider.CompareStat(oldStat, newStat, expected);
}
