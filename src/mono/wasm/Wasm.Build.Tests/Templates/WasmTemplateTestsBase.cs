// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    private readonly string _extraBuildArgsPublish = "-p:CompressionEnabled=false";
    protected readonly PublishOptions _defaultPublishOptions;
    protected readonly BuildOptions _defaultBuildOptions;
    protected const string DefaultRuntimeAssetsRelativePath = "./_framework/";

    public WasmTemplateTestsBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext, ProjectProviderBase? provider = null)
        : base(provider ?? new WasmSdkBasedProjectProvider(output, DefaultTargetFramework), output, buildContext)
    {
        _provider = GetProvider<WasmSdkBasedProjectProvider>();
        _defaultPublishOptions = new PublishOptions(ExtraMSBuildArgs: _extraBuildArgsPublish);
        _defaultBuildOptions = new BuildOptions();
    }

    private Dictionary<string, string> browserProgramReplacements = new Dictionary<string, string>
        {
            { "while (true)", $"int i = 0;{Environment.NewLine}while (i++ < 0)" },  // the test has to be fast, skip the loop
            { "partial class StopwatchSample", $"return 42;{Environment.NewLine}partial class StopwatchSample" },
            { "Hello, Browser!", "TestOutput -> Hello, Browser!" }
        };

    private string GetProjectName(string idPrefix, Configuration config, bool aot, bool appendUnicodeToPath, bool avoidAotLongPathIssue = false) =>
        avoidAotLongPathIssue ? // https://github.com/dotnet/runtime/issues/103625
            $"{GetRandomId()}" :
            appendUnicodeToPath ?
                $"{idPrefix}_{config}_{aot}_{GetRandomId()}_{s_unicodeChars}" :
                $"{idPrefix}_{config}_{aot}_{GetRandomId()}";

    private (string projectName, string logPath, string nugetDir) InitProjectLocation(string idPrefix, Configuration config, bool aot, bool appendUnicodeToPath, bool avoidAotLongPathIssue = false)
    {
        string projectName = GetProjectName(idPrefix, config, aot, appendUnicodeToPath, avoidAotLongPathIssue);
        (string logPath, string nugetDir) = InitPaths(projectName);
        InitProjectDir(_projectDir, addNuGetSourceForLocalPackages: true);
        return (projectName, logPath, nugetDir);
    }

    public ProjectInfo CreateWasmTemplateProject(
        Template template,
        Configuration config,
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
        (string projectName, string logPath, string nugetDir) =
            InitProjectLocation(idPrefix, config, aot, appendUnicodeToPath);

        if (addFrameworkArg)
            extraArgs += $" -f {DefaultTargetFramework}";

        using DotNetCommand cmd = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false);
        CommandResult result = cmd.WithWorkingDirectory(_projectDir)
            .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
            .ExecuteWithCapturedOutput($"new {template.ToString().ToLower()} {extraArgs}")
            .EnsureSuccessful();

        UpdateBootJsInHtmlFiles();

        string projectFilePath = Path.Combine(_projectDir, $"{projectName}.csproj");
        UpdateProjectFile(projectFilePath, runAnalyzers, extraProperties, extraItems, insertAtEnd);
        return new ProjectInfo(projectName, projectFilePath, logPath, nugetDir);
    }

    protected void UpdateBootJsInHtmlFiles()
    {
        foreach (var filePath in Directory.EnumerateFiles(_projectDir, "*.html", SearchOption.AllDirectories))
        {
            UpdateBootJsInHtmlFile(filePath);
        }
    }

    protected void UpdateBootJsInHtmlFile(string filePath)
    {
        string fileContent = File.ReadAllText(filePath);
        fileContent = StringReplaceWithAssert(fileContent, "<head>", "<head><script>window['__DOTNET_INTERNAL_BOOT_CONFIG_SRC'] = 'dotnet.boot.js';</script>");
        File.WriteAllText(filePath, fileContent);
    }

    protected ProjectInfo CopyTestAsset(
        Configuration config,
        bool aot,
        TestAsset asset,
        string idPrefix,
        bool appendUnicodeToPath = true,
        bool runAnalyzers = true,
        string extraProperties = "",
        string extraItems = "",
        string insertAtEnd = "")
    {
        (string projectName, string logPath, string nugetDir) =
            InitProjectLocation(idPrefix, config, aot, appendUnicodeToPath, avoidAotLongPathIssue: s_isWindows && aot);
        Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, asset.Name), Path.Combine(_projectDir));
        if (!string.IsNullOrEmpty(asset.RunnableProjectSubPath))
        {
            _projectDir = Path.Combine(_projectDir, asset.RunnableProjectSubPath);
        }
        string projectFilePath = Path.Combine(_projectDir, $"{asset.Name}.csproj");
        UpdateProjectFile(projectFilePath, runAnalyzers, extraProperties, extraItems, insertAtEnd);
        return new ProjectInfo(asset.Name, projectFilePath, logPath, nugetDir);
    }

    private void UpdateProjectFile(string projectFilePath, bool runAnalyzers, string extraProperties, string extraItems, string insertAtEnd)
    {
        extraProperties += "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>";
        if (runAnalyzers)
            extraProperties += "<RunAnalyzers>true</RunAnalyzers>";
        AddItemsPropertiesToProject(projectFilePath, extraProperties, extraItems, insertAtEnd);
    }

    public virtual (string projectDir, string buildOutput) PublishProject(
        ProjectInfo info,
        Configuration configuration,
        bool? isNativeBuild = null) => // null for WasmBuildNative unset
        BuildProjectCore(info, configuration, _defaultPublishOptions, isNativeBuild);

    public virtual (string projectDir, string buildOutput) PublishProject(
        ProjectInfo info,
        Configuration configuration,
        PublishOptions publishOptions,
        bool? isNativeBuild = null) =>
        BuildProjectCore(
            info,
            configuration,
            publishOptions with { ExtraMSBuildArgs = $"{_extraBuildArgsPublish} {publishOptions.ExtraMSBuildArgs}" },
            isNativeBuild
        );

    public virtual (string projectDir, string buildOutput) BuildProject(
        ProjectInfo info,
        Configuration configuration,
        bool? isNativeBuild = null) => // null for WasmBuildNative unset
        BuildProjectCore(info, configuration, _defaultBuildOptions, isNativeBuild);

    public virtual (string projectDir, string buildOutput) BuildProject(
        ProjectInfo info,
        Configuration configuration,
        BuildOptions buildOptions,
        bool? isNativeBuild = null) =>
        BuildProjectCore(info, configuration, buildOptions, isNativeBuild);

    private (string projectDir, string buildOutput) BuildProjectCore(
        ProjectInfo info,
        Configuration configuration,
        MSBuildOptions buildOptions,
        bool? isNativeBuild = null)
    {
        if (buildOptions.AOT)
        {
            buildOptions = buildOptions with { ExtraMSBuildArgs = $"{buildOptions.ExtraMSBuildArgs} -p:RunAOTCompilation=true -p:EmccVerbose=true" };
        }

        if (buildOptions.ExtraBuildEnvironmentVariables is null)
            buildOptions = buildOptions with { ExtraBuildEnvironmentVariables = new Dictionary<string, string>() };

        buildOptions.ExtraBuildEnvironmentVariables["TreatPreviousAsCurrent"] = "false";

        buildOptions = buildOptions with { ExtraMSBuildArgs = $"{buildOptions.ExtraMSBuildArgs} -p:WasmBootConfigFileName={buildOptions.BootConfigFileName}" };

        (CommandResult res, string logFilePath) = BuildProjectWithoutAssert(configuration, info.ProjectName, buildOptions);

        if (buildOptions.UseCache)
            _buildContext.CacheBuild(info, new BuildResult(_projectDir, logFilePath, true, res.Output));

        if (!buildOptions.ExpectSuccess)
        {
            res.EnsureFailed();
            return (_projectDir, res.Output);
        }

        if (buildOptions.AssertAppBundle)
        {
            _provider.AssertWasmSdkBundle(configuration, buildOptions, IsUsingWorkloads, isNativeBuild, res.Output);
        }
        return (_projectDir, res.Output);
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
        var path = Path.Combine(_projectDir, pathRelativeToProjectDir);
        string text = File.ReadAllText(path);
        foreach (var replacement in replacements)
        {
            text = StringReplaceWithAssert(text, replacement.Key, replacement.Value);
        }
        File.WriteAllText(path, text);
    }

    protected void UpdateFile(string pathRelativeToProjectDir, string newContent)
    {
        var updatedFilePath = Path.Combine(_projectDir, pathRelativeToProjectDir);
        File.WriteAllText(updatedFilePath, newContent);
    }

    protected void ReplaceFile(string pathRelativeToProjectDir, string pathWithNewContent)
    {
        string newContent = File.ReadAllText(pathWithNewContent);
        UpdateFile(pathRelativeToProjectDir, newContent);
    }

    protected void DeleteFile(string pathRelativeToProjectDir)
    {
        var deletedFilePath = Path.Combine(_projectDir, pathRelativeToProjectDir);
        if (File.Exists(deletedFilePath))
        {
            File.Delete(deletedFilePath);
        }
    }

    protected void UpdateBrowserMainJs(string? targetFramework = null, string runtimeAssetsRelativePath = DefaultRuntimeAssetsRelativePath)
    {
        targetFramework ??= DefaultTargetFramework;
        string mainJsPath = Path.Combine(_projectDir, "wwwroot", "main.js");
        string mainJsContent = File.ReadAllText(mainJsPath);
        Version targetFrameworkVersion = new Version(targetFramework.Replace("net", ""));

        string updatedMainJsContent = StringReplaceWithAssert(
            mainJsContent,
            ".create()",
            (targetFrameworkVersion.Major >= 8)
                    ? ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().withExitOnUnhandledError().create()"
                    : ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().create()"
            );

        // dotnet.run() is used instead of runMain() in net9.0+
        if (targetFrameworkVersion.Major >= 9)
            updatedMainJsContent = StringReplaceWithAssert(updatedMainJsContent, "runMain()", "dotnet.run()");

        updatedMainJsContent = StringReplaceWithAssert(updatedMainJsContent, "from './_framework/dotnet.js'", $"from '{runtimeAssetsRelativePath}dotnet.js'");


        File.WriteAllText(mainJsPath, updatedMainJsContent);
    }

    // Keeping these methods with explicit Build/Publish in the name
    // so in the test code it is evident which is being run!
    public virtual async Task<RunResult> RunForBuildWithDotnetRun(RunOptions runOptions)
        => await BrowserRun(runOptions with { Host = RunHost.DotnetRun });

    public virtual async Task<RunResult> RunForPublishWithWebServer(RunOptions runOptions)
        => await BrowserRun(runOptions with { Host = RunHost.WebServer });

    private async Task<RunResult> BrowserRun(RunOptions runOptions) => runOptions.Host switch
    {
        RunHost.DotnetRun =>
                await BrowserRunTest($"run -c {runOptions.Configuration} --no-build", _projectDir, runOptions),

        RunHost.WebServer =>
                await BrowserRunTest($"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files",
                    string.IsNullOrEmpty(runOptions.CustomBundleDir) ?
                        Path.GetFullPath(Path.Combine(GetBinFrameworkDir(runOptions.Configuration, forPublish: true), "..")) :
                        runOptions.CustomBundleDir,
                     runOptions),

        _ => throw new NotImplementedException(runOptions.Host.ToString())
    };

    private async Task<RunResult> BrowserRunTest(string runArgs,
                                    string workingDirectory,
                                    RunOptions runOptions)
    {
        if (!string.IsNullOrEmpty(runOptions.ExtraArgs))
            runArgs += $" {runOptions.ExtraArgs}";

        runOptions.ServerEnvironment?.ToList().ForEach(
            kv => s_buildEnv.EnvVars[kv.Key] = kv.Value);

        using RunCommand runCommand = new RunCommand(s_buildEnv, _testOutput);
        ToolCommand cmd = runCommand.WithWorkingDirectory(workingDirectory);

        var query = runOptions.BrowserQueryString ?? new NameValueCollection();
        if (runOptions.AOT)
        {
            query.Add("MONO_LOG_LEVEL", "debug");
            query.Add("MONO_LOG_MASK", "aot");
        }
        if (runOptions is BrowserRunOptions browserOp && !string.IsNullOrEmpty(browserOp.TestScenario))
            query.Add("test", browserOp.TestScenario);
        var queryString = query.Count > 0 && query.AllKeys != null
            ? "?" + string.Join("&", query.AllKeys.SelectMany(key => query.GetValues(key)?.Select(value => $"{key}={value}") ?? Enumerable.Empty<string>()))
            : "";

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

        if (runOptions is BlazorRunOptions blazorOp && blazorOp.Test is not null)
            await blazorOp.Test(page);

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

    public string GetBinFrameworkDir(Configuration config, bool forPublish, string? framework = null, string? projectDir = null) =>
        _provider.GetBinFrameworkDir(config, forPublish, framework ?? DefaultTargetFramework, projectDir);

    public BuildPaths GetBuildPaths(Configuration config, bool forPublish) =>
        _provider.GetBuildPaths(config, forPublish);

    public IDictionary<string, (string fullPath, bool unchanged)> GetFilesTable(string projectName, bool isAOT, BuildPaths paths, bool unchanged) =>
        _provider.GetFilesTable(projectName, isAOT, paths, unchanged);

    public IDictionary<string, FileStat> StatFiles(IDictionary<string, (string fullPath, bool unchanged)> fullpaths) =>
        _provider.StatFiles(fullpaths);

    // 2nd and next stats with fingerprinting require updated statistics
    public IDictionary<string, FileStat> StatFilesAfterRebuild(IDictionary<string, (string fullPath, bool unchanged)> fullpaths) =>
        _provider.StatFilesAfterRebuild(fullpaths);

    public void CompareStat(IDictionary<string, FileStat> oldStat, IDictionary<string, FileStat> newStat, IDictionary<string, (string fullPath, bool unchanged)> expected) =>
        _provider.CompareStat(oldStat, newStat, expected);
}
