// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public class WasmLegacyTemplateTests : BuildTestBase
{
    public WasmLegacyTemplateTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    private void UpdateBrowserMainJs(string targetFramework)
    {
        string mainJsPath = Path.Combine(_projectDir!, "main.js");
        string mainJsContent = File.ReadAllText(mainJsPath);

        // FIXME: withConsoleForwarding - use only with wasm app host
        // .withExitOnUnhandledError() is available only only >net7.0
        mainJsContent = mainJsContent.Replace(".create()",
                targetFramework == "net8.0"
                    ? ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().withExitOnUnhandledError().create()"
                    : ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().create()");
        File.WriteAllText(mainJsPath, mainJsContent);
    }

    private void UpdateMainJsEnvironmentVariables(params (string key, string value)[] variables)
    {
        string mainJsPath = Path.Combine(_projectDir!, "main.mjs");
        string mainJsContent = File.ReadAllText(mainJsPath);

        StringBuilder js = new();
        foreach (var variable in variables)
        {
            js.Append($".withEnvironmentVariable(\"{variable.key}\", \"{variable.value}\")");
        }

        mainJsContent = mainJsContent
            .Replace(".create()", js.ToString() + ".create()");

        File.WriteAllText(mainJsPath, mainJsContent);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void BrowserBuildThenPublish(string config)
    {
        string id = $"browser_{config}_{Path.GetRandomFileName()}";
        string projectFile = CreateWasmTemplateProjectFromAssets(id, "wasmbrowser-legacy");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);

        UpdateBrowserMainJs(DefaultTargetFramework);

        var buildArgs = new BuildArgs(projectName, config, false, id, null);
        buildArgs = ExpandBuildArgs(buildArgs);

        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: true,
                        CreateProject: false,
                        HasV8Script: false,
                        MainJS: "main.js",
                        Publish: false,
                        TargetFramework: DefaultTargetFramework,
                        FromTemplate: WasmTemplate.wasmbrowser_legacy
                    ));

        // FIXME: AJ: disabled for non-legacy wasmbrowser
        AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: true, targetFramework: DefaultTargetFramework);

        if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
            throw new XunitException($"Test bug: could not get the build product in the cache");

        File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

        _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

        bool expectRelinking = config == "Release";
        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: !expectRelinking,
                        CreateProject: false,
                        HasV8Script: false,
                        MainJS: "main.js",
                        Publish: true,
                        TargetFramework: DefaultTargetFramework,
                        UseCache: false,
                        FromTemplate: WasmTemplate.wasmbrowser_legacy));

        AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: !expectRelinking, targetFramework: DefaultTargetFramework);
    }

    public static TheoryData<bool, bool, string> TestDataForAppBundleDir()
    {
        var data = new TheoryData<bool, bool, string>();
        AddTestData(forConsole: true, runOutsideProjectDirectory: false);
        AddTestData(forConsole: true, runOutsideProjectDirectory: true);

        // AddTestData(forConsole: false, runOutsideProjectDirectory: false);
        // AddTestData(forConsole: false, runOutsideProjectDirectory: true);

        void AddTestData(bool forConsole, bool runOutsideProjectDirectory)
        {
            // FIXME: Disabled for `main` right now, till 7.0 gets the fix
            data.Add(runOutsideProjectDirectory, forConsole, string.Empty);

            data.Add(runOutsideProjectDirectory, forConsole,
                            $"<OutputPath>{Path.Combine(BuildEnvironment.TmpPath, Path.GetRandomFileName())}</OutputPath>");
            data.Add(runOutsideProjectDirectory, forConsole,
                            $"<WasmAppDir>{Path.Combine(BuildEnvironment.TmpPath, Path.GetRandomFileName())}</WasmAppDir>");
        }

        return data;
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
    [MemberData(nameof(TestDataForAppBundleDir))]
    public Task RunWithDifferentAppBundleLocations(bool forConsole, bool runOutsideProjectDirectory, string extraProperties)
        => forConsole // ignore console tests because they haven't upgraded to use wasm sdk yet
                ? Task.CompletedTask
                : BrowserRunTwiceWithAndThenWithoutBuildAsync("Release", extraProperties, runOutsideProjectDirectory);

    private async Task BrowserRunTwiceWithAndThenWithoutBuildAsync(string config, string extraProperties = "", bool runOutsideProjectDirectory = false)
    {
        string id = $"browser_{config}_{Path.GetRandomFileName()}";
        string projectFile = CreateWasmTemplateProjectFromAssets(id, "wasmbrowser-legacy");

        UpdateBrowserMainJs(DefaultTargetFramework);

        if (!string.IsNullOrEmpty(extraProperties))
            AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

        string workingDir = runOutsideProjectDirectory ? BuildEnvironment.TmpPath : _projectDir!;

        {
            using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(workingDir);

            await using var runner = new BrowserRunner(_testOutput);
            var page = await runner.RunAsync(runCommand, $"run -c {config} --project {projectFile} --forward-console");
            await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
            Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
        }

        {
            using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(workingDir);

            await using var runner = new BrowserRunner(_testOutput);
            var page = await runner.RunAsync(runCommand, $"run -c {config} --no-build --project {projectFile} --forward-console");
            await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
            Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
        }
    }

    [ConditionalFact(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
    public async Task BrowserBuildAndRun()
    {
        string config = "Debug";
        string id = $"browser_{config}_{Path.GetRandomFileName()}";
        CreateWasmTemplateProjectFromAssets(id, "wasmbrowser-legacy");

        UpdateBrowserMainJs("net8.0");

        new DotNetCommand(s_buildEnv, _testOutput)
                .WithWorkingDirectory(_projectDir!)
                .Execute($"build -c {config} -bl:{Path.Combine(s_buildEnv.LogRootPath, $"{id}.binlog")}")
                .EnsureSuccessful();

        using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!);

        await using var runner = new BrowserRunner(_testOutput);
        var page = await runner.RunAsync(runCommand, $"run -c {config} --no-build -r browser-wasm --forward-console");
        await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
        Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
    }

    private string CreateWasmTemplateProjectFromAssets(string id, string testAssetName, bool runAnalyzers = true)
    {
        InitPaths(id);
        InitProjectDir(_projectDir, addNuGetSourceForLocalPackages: true);
        _testOutput.WriteLine($"id: {id}, _projectDir: {_projectDir}");

        File.WriteAllText(Path.Combine(_projectDir, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(_projectDir, "Directory.Build.targets"),
            """
            <Project>
              <Target Name="PrintRuntimePackPath" BeforeTargets="Build">
                  <Message Text="** MicrosoftNetCoreAppRuntimePackDir : '@(ResolvedRuntimePack -> '%(PackageDirectory)')'" Importance="High" Condition="@(ResolvedRuntimePack->Count()) > 0" />
              </Target>
            </Project>
            """);

        Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, testAssetName), _projectDir);
        File.Move(Path.Combine(_projectDir!, $"{testAssetName}.csproj"), Path.Combine(_projectDir!, $"{id}.csproj"));

        string projectfile = Path.Combine(_projectDir!, $"{id}.csproj");
        string extraProperties = string.Empty;
        extraProperties += "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>";
        if (runAnalyzers)
            extraProperties += "<RunAnalyzers>true</RunAnalyzers>";
        if (UseWebcil)
            extraProperties += "<WasmEnableWebcil>true</WasmEnableWebcil>";

        AddItemsPropertiesToProject(projectfile, extraProperties);

        return projectfile;
    }
}

