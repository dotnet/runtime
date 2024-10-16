// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            { "while(true)", $"int i = 0;{Environment.NewLine}while(i++ < 10)" },
            { "partial class StopwatchSample", $"return 42;{Environment.NewLine}partial class StopwatchSample" }
        };

    public string CreateWasmTemplateProject(string id, string template = "wasmbrowser", string extraArgs = "", bool runAnalyzers = true, bool addFrameworkArg = false, string? extraProperties = null)
    {
        InitPaths(id);
        InitProjectDir(_projectDir, addNuGetSourceForLocalPackages: true);

        File.WriteAllText(Path.Combine(_projectDir, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(_projectDir, "Directory.Build.targets"),
            """
            <Project>
              <Target Name="PrintRuntimePackPath" BeforeTargets="Build">
                  <Message Text="** MicrosoftNetCoreAppRuntimePackDir : '@(ResolvedRuntimePack -> '%(PackageDirectory)')'" Importance="High" Condition="@(ResolvedRuntimePack->Count()) > 0" />
              </Target>

              <Import Project="WasmOverridePacks.targets" Condition="'$(WBTOverrideRuntimePack)' == 'true'" />
            </Project>
            """);
        if (UseWBTOverridePackTargets)
            File.Copy(BuildEnvironment.WasmOverridePacksTargetsPath, Path.Combine(_projectDir, Path.GetFileName(BuildEnvironment.WasmOverridePacksTargetsPath)), overwrite: true);

        if (addFrameworkArg)
            extraArgs += $" -f {DefaultTargetFramework}";
        using DotNetCommand cmd = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false);
        CommandResult result = cmd.WithWorkingDirectory(_projectDir!)
            .ExecuteWithCapturedOutput($"new {template} {extraArgs}")
            .EnsureSuccessful();

        string projectfile = Path.Combine(_projectDir!, $"{id}.csproj");

        if (extraProperties == null)
            extraProperties = string.Empty;

        extraProperties += "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>";
        if (runAnalyzers)
            extraProperties += "<RunAnalyzers>true</RunAnalyzers>";

        AddItemsPropertiesToProject(projectfile, extraProperties);

        return projectfile;
    }

    public (string projectDir, string buildOutput) BuildTemplateProject(
        BuildArgs buildArgs,
        string id,
        BuildProjectOptions buildProjectOptions,
        params string[] extraArgs)
    {
        if (buildProjectOptions.ExtraBuildEnvironmentVariables is null)
            buildProjectOptions = buildProjectOptions with { ExtraBuildEnvironmentVariables = new Dictionary<string, string>() };

        // TODO: reenable this when the SDK supports targetting net10.0
        //buildProjectOptions.ExtraBuildEnvironmentVariables["TreatPreviousAsCurrent"] = "false";

        (CommandResult res, string logFilePath) = BuildProjectWithoutAssert(id, buildArgs.Config, buildProjectOptions, extraArgs);
        if (buildProjectOptions.UseCache)
            _buildContext.CacheBuild(buildArgs, new BuildProduct(_projectDir!, logFilePath, true, res.Output));

        if (buildProjectOptions.AssertAppBundle)
        {
            if (buildProjectOptions.IsBrowserProject)
            {
                _provider.AssertWasmSdkBundle(buildArgs, buildProjectOptions, res.Output);
            }
            else
            {
                _provider.AssertTestMainJsBundle(buildArgs, buildProjectOptions, res.Output);
            }
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

    protected void UpdateMainJsEnvironmentVariables(params (string key, string value)[] variables)
    {
        string mainJsPath = Path.Combine(_projectDir!, "main.mjs");
        string mainJsContent = File.ReadAllText(mainJsPath);

        StringBuilder js = new();
        foreach (var variable in variables)
        {
            js.Append($".withEnvironmentVariable(\"{variable.key}\", \"{variable.value}\")");
        }

        mainJsContent = StringReplaceWithAssert(mainJsContent, ".create()", js.ToString() + ".create()");

        File.WriteAllText(mainJsPath, mainJsContent);
    }

    // ToDo: consolidate with BlazorRunTest
    protected async Task<string> RunBuiltBrowserApp(string config, string projectFile, string language = "en-US", string extraArgs = "", string testScenario = "")
        => await RunBrowser(
            $"run --no-silent -c {config} --no-build --project \"{projectFile}\" --forward-console {extraArgs}",
            _projectDir!,
            language,
            testScenario: testScenario);

    protected async Task<string> RunPublishedBrowserApp(string config, string language = "en-US", string extraArgs = "", string testScenario = "")
        => await RunBrowser(
            command: $"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files",
            workingDirectory: Path.Combine(FindBinFrameworkDir(config, forPublish: true), ".."),
            language: language,
            testScenario: testScenario);

    private async Task<string> RunBrowser(string command, string workingDirectory, string language = "en-US", string testScenario = "")
    {
        using var runCommand = new RunCommand(s_buildEnv, _testOutput).WithWorkingDirectory(workingDirectory);
        await using var runner = new BrowserRunner(_testOutput);
        Func<string, string>? modifyBrowserUrl = string.IsNullOrEmpty(testScenario) ?
            null :
            browserUrl => new Uri(new Uri(browserUrl), $"?test={testScenario}").ToString();
        var page = await runner.RunAsync(runCommand, command, language: language, modifyBrowserUrl: modifyBrowserUrl);
        await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
        Assert.Contains("WASM EXIT 42", string.Join(Environment.NewLine, runner.OutputLines));
        return string.Join("\n", runner.OutputLines);
    }

    public string FindBinFrameworkDir(string config, bool forPublish, string framework = DefaultTargetFramework, string? projectDir = null) =>
        _provider.FindBinFrameworkDir(config: config, forPublish: forPublish, framework: framework, projectDir: projectDir);
}
