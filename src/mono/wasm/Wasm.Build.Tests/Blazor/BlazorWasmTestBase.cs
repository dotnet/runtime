// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Wasm.Build.Tests;

public abstract class BlazorWasmTestBase : WasmTemplateTestBase
{
    protected BlazorWasmProjectProvider _provider;
    protected BlazorWasmTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(output, buildContext, new BlazorWasmProjectProvider(output))
    {
        _provider = GetProvider<BlazorWasmProjectProvider>();
    }

    public void InitBlazorWasmProjectDir(string id, string targetFramework = DefaultTargetFrameworkForBlazor)
    {
        InitPaths(id);
        if (Directory.Exists(_projectDir))
            Directory.Delete(_projectDir, recursive: true);
        Directory.CreateDirectory(_projectDir);

        File.WriteAllText(Path.Combine(_projectDir, "nuget.config"),
                            GetNuGetConfigWithLocalPackagesPath(
                                        GetNuGetConfigPathFor(targetFramework),
                                        s_buildEnv.BuiltNuGetsPath));

        File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.props"), Path.Combine(_projectDir, "Directory.Build.props"));
        File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.targets"), Path.Combine(_projectDir, "Directory.Build.targets"));
    }

    public string CreateBlazorWasmTemplateProject(string id)
    {
        InitBlazorWasmProjectDir(id);
        new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false)
                .WithWorkingDirectory(_projectDir!)
                .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                .ExecuteWithCapturedOutput("new blazorwasm")
                .EnsureSuccessful();

        string projectFile = Path.Combine(_projectDir!, $"{id}.csproj");
        if (!UseWebcil)
            AddItemsPropertiesToProject(projectFile, "<WasmEnableWebcil>false</WasmEnableWebcil>");
        return projectFile;
    }

    protected (CommandResult, string) BlazorBuild(BlazorBuildOptions options, params string[] extraArgs)
    {
        if (options.WarnAsError)
            extraArgs = extraArgs.Append("/warnaserror").ToArray();

        var res = BlazorBuildInternal(options.Id, options.Config, publish: false, setWasmDevel: false, extraArgs);
        _provider.AssertBlazorBundle(options, isPublish: false);

        return res;
    }

    protected (CommandResult, string) BlazorPublish(BlazorBuildOptions options, params string[] extraArgs)
    {
        var res = BlazorBuildInternal(options.Id, options.Config, publish: true, setWasmDevel: false, extraArgs);
        _provider.AssertBlazorBundle(options, isPublish: true);

        if (options.ExpectedFileType == NativeFilesType.AOT)
        {
            // check for this too, so we know the format is correct for the negative
            // test for jsinterop.webassembly.dll
            Assert.Contains("Microsoft.JSInterop.dll -> Microsoft.JSInterop.dll.bc", res.Item1.Output);

            // make sure this assembly gets skipped
            Assert.DoesNotContain("Microsoft.JSInterop.WebAssembly.dll -> Microsoft.JSInterop.WebAssembly.dll.bc", res.Item1.Output);
        }

        string objBuildDir = Path.Combine(_projectDir!, "obj", options.Config, options.TargetFramework, "wasm", "for-build");
        // Check that we linked only for publish
        if (options.ExpectRelinkDirWhenPublishing)
            Assert.True(Directory.Exists(objBuildDir), $"Could not find expected {objBuildDir}, which gets created when relinking during Build. This is likely a test authoring error");
        else
            Assert.False(Directory.Exists(objBuildDir), $"Found unexpected {objBuildDir}, which gets created when relinking during Build");

        return res;
    }

    protected (CommandResult, string) BlazorBuildInternal(string id, string config, bool publish = false, bool setWasmDevel = true, params string[] extraArgs)
    {
        string label = publish ? "publish" : "build";
        _testOutput.WriteLine($"{Environment.NewLine}** {label} **{Environment.NewLine}");

        string logPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-{label}.binlog");
        string[] combinedArgs = new[]
        {
            label, // same as the command name
            $"-bl:{logPath}",
            $"-p:Configuration={config}",
            !UseWebcil ? "-p:WasmEnableWebcil=false" : string.Empty,
            "-p:BlazorEnableCompression=false",
            "-nr:false",
            setWasmDevel ? "-p:_WasmDevel=true" : string.Empty
        }.Concat(extraArgs).ToArray();

        CommandResult res = new DotNetCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!)
                                    .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                                    .ExecuteWithCapturedOutput(combinedArgs)
                                    .EnsureSuccessful();

        return (res, logPath);
    }

    protected string CreateProjectWithNativeReference(string id)
    {
        CreateBlazorWasmTemplateProject(id);

        string extraItems = @$"
            {GetSkiaSharpReferenceItems()}
            <WasmFilesToIncludeInFileSystem Include=""{Path.Combine(BuildEnvironment.TestAssetsPath, "mono.png")}"" />
        ";
        string projectFile = Path.Combine(_projectDir!, $"{id}.csproj");
        AddItemsPropertiesToProject(projectFile, extraItems: extraItems);

        return projectFile;
    }

    // Keeping these methods with explicit Build/Publish in the name
    // so in the test code it is evident which is being run!
    public Task BlazorRunForBuildWithDotnetRun(string config, Func<IPage, Task>? test = null, string extraArgs = "--no-build", Action<IConsoleMessage>? onConsoleMessage = null)
        => BlazorRunTest($"run -c {config} {extraArgs}", _projectDir!, test, onConsoleMessage);

    public Task BlazorRunForPublishWithWebServer(string config, Func<IPage, Task>? test = null, string extraArgs = "", Action<IConsoleMessage>? onConsoleMessage = null)
        => BlazorRunTest($"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files {extraArgs}",
                         Path.GetFullPath(Path.Combine(FindBlazorBinFrameworkDir(config, forPublish: true), "..")),
                         test, onConsoleMessage);

    public async Task BlazorRunTest(string runArgs,
                                    string workingDirectory,
                                    Func<IPage, Task>? test = null,
                                    Action<IConsoleMessage>? onConsoleMessage = null,
                                    bool detectRuntimeFailures = true)
    {
        using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(workingDirectory);

        await using var runner = new BrowserRunner(_testOutput);
        var page = await runner.RunAsync(runCommand, runArgs, onConsoleMessage: OnConsoleMessage);

        await page.Locator("text=Counter").ClickAsync();
        var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
        Assert.Equal("Current count: 0", txt);

        await page.Locator("text=\"Click me\"").ClickAsync();
        txt = await page.Locator("p[role='status']").InnerHTMLAsync();
        Assert.Equal("Current count: 1", txt);

        if (test is not null)
            await test(page);

        void OnConsoleMessage(IConsoleMessage msg)
        {
            _testOutput.WriteLine($"[{msg.Type}] {msg.Text}");

            onConsoleMessage?.Invoke(msg);

            if (detectRuntimeFailures)
            {
                if (msg.Text.Contains("[MONO] * Assertion") || msg.Text.Contains("Error: [MONO] "))
                    throw new XunitException($"Detected a runtime failure at line: {msg.Text}");
            }
        }
    }

}
