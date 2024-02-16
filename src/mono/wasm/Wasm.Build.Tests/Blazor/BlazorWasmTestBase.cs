// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Wasm.Build.Tests.Blazor;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Wasm.Build.Tests;

public abstract class BlazorWasmTestBase : WasmTemplateTestBase
{
    protected readonly BlazorWasmProjectProvider _provider;
    protected BlazorWasmTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(output, buildContext, new BlazorWasmProjectProvider(output))
    {
        _provider = GetProvider<BlazorWasmProjectProvider>();
        _provider.BundleDirName = "wwwroot";
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
        if (UseWBTOverridePackTargets)
            File.Copy(BuildEnvironment.WasmOverridePacksTargetsPath, Path.Combine(_projectDir, Path.GetFileName(BuildEnvironment.WasmOverridePacksTargetsPath)), overwrite: true);
    }

    public string CreateBlazorWasmTemplateProject(string id)
    {
        InitBlazorWasmProjectDir(id);
        new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false)
                .WithWorkingDirectory(_projectDir!)
                .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                .ExecuteWithCapturedOutput("new blazorwasm")
                .EnsureSuccessful();

        return Path.Combine(_projectDir!, $"{id}.csproj");
    }

    protected (CommandResult, string) BlazorBuild(BlazorBuildOptions options, params string[] extraArgs)
    {
        if (options.WarnAsError)
            extraArgs = extraArgs.Append("/warnaserror").ToArray();

        (CommandResult res, string logPath) = BlazorBuildInternal(options.Id, options.Config, publish: false, setWasmDevel: false, expectSuccess: options.ExpectSuccess, extraArgs);

        if (options.ExpectSuccess && options.AssertAppBundle)
        {
            AssertBundle(res.Output, options with { IsPublish = false });
        }

        return (res, logPath);
    }

    protected (CommandResult, string) BlazorPublish(BlazorBuildOptions options, params string[] extraArgs)
    {
        if (options.WarnAsError)
            extraArgs = extraArgs.Append("/warnaserror").ToArray();

        (CommandResult res, string logPath) = BlazorBuildInternal(options.Id, options.Config, publish: true, setWasmDevel: false, expectSuccess: options.ExpectSuccess, extraArgs);

        if (options.ExpectSuccess && options.AssertAppBundle)
        {
            // Because we do relink in Release publish by default
            if (options.Config == "Release")
                options = options with { ExpectedFileType = NativeFilesType.Relinked };

            AssertBundle(res.Output, options with { IsPublish = true });
        }

        return (res, logPath);
    }

    protected (CommandResult res, string logPath) BlazorBuildInternal(
        string id,
        string config,
        bool publish = false,
        bool setWasmDevel = true,
        bool expectSuccess = true,
        params string[] extraArgs)
    {
        try
        {
            return BuildProjectWithoutAssert(
                        id,
                        config,
                        new BuildProjectOptions(CreateProject: false, UseCache: false, Publish: publish, ExpectSuccess: expectSuccess),
                        extraArgs.Concat(new[]
                        {
                            "-p:BlazorEnableCompression=false",
                            setWasmDevel ? "-p:_WasmDevel=true" : string.Empty
                        }).ToArray());
        }
        catch (XunitException xe)
        {
            if (xe.Message.Contains("error CS1001: Identifier expected"))
                Utils.DirectoryCopy(_projectDir!, Path.Combine(s_buildEnv.LogRootPath, id), testOutput: _testOutput);
            throw;
        }
    }

    public void AssertBundle(string buildOutput, BlazorBuildOptions blazorBuildOptions)
    {
        if (IsUsingWorkloads)
        {
            // In no-workload case, the path would be from a restored nuget
            ProjectProviderBase.AssertRuntimePackPath(buildOutput, blazorBuildOptions.TargetFramework ?? DefaultTargetFramework, blazorBuildOptions.RuntimeType);
        }

        _provider.AssertBundle(blazorBuildOptions);

        if (!blazorBuildOptions.IsPublish)
            return;

        // Publish specific checks

        if (blazorBuildOptions.ExpectedFileType == NativeFilesType.AOT)
        {
            // check for this too, so we know the format is correct for the negative
            // test for jsinterop.webassembly.dll
            Assert.Contains("Microsoft.JSInterop.dll -> Microsoft.JSInterop.dll.bc", buildOutput);

            // make sure this assembly gets skipped
            Assert.DoesNotContain("Microsoft.JSInterop.WebAssembly.dll -> Microsoft.JSInterop.WebAssembly.dll.bc", buildOutput);
        }

        string objBuildDir = Path.Combine(_projectDir!, "obj", blazorBuildOptions.Config, blazorBuildOptions.TargetFramework!, "wasm", "for-build");
        // Check that we linked only for publish
        if (blazorBuildOptions.ExpectRelinkDirWhenPublishing)
            Assert.True(Directory.Exists(objBuildDir), $"Could not find expected {objBuildDir}, which gets created when relinking during Build. This is likely a test authoring error");
        else
            Assert.False(File.Exists(Path.Combine(objBuildDir, "emcc-link.rsp")), $"Found unexpected `emcc-link.rsp` in {objBuildDir}, which gets created when relinking during Build.");
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
    public Task BlazorRunForBuildWithDotnetRun(BlazorRunOptions runOptions)
        => BlazorRunTest(runOptions with { Host = BlazorRunHost.DotnetRun });

    public Task BlazorRunForPublishWithWebServer(BlazorRunOptions runOptions)
        => BlazorRunTest(runOptions with { Host = BlazorRunHost.WebServer });

    public Task BlazorRunTest(BlazorRunOptions runOptions) => runOptions.Host switch
    {
        BlazorRunHost.DotnetRun =>
                BlazorRunTest($"run -c {runOptions.Config} --no-build", _projectDir!, runOptions),

        BlazorRunHost.WebServer =>
                BlazorRunTest($"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files",
                     Path.GetFullPath(Path.Combine(FindBlazorBinFrameworkDir(runOptions.Config, forPublish: true), "..")),
                     runOptions),

        _ => throw new NotImplementedException(runOptions.Host.ToString())
    };

    public async Task BlazorRunTest(string runArgs,
                                    string workingDirectory,
                                    BlazorRunOptions runOptions)
    {
        if (!string.IsNullOrEmpty(runOptions.ExtraArgs))
            runArgs += $" {runOptions.ExtraArgs}";
        using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(workingDirectory);

        await using var runner = new BrowserRunner(_testOutput);
        var page = await runner.RunAsync(runCommand, runArgs, onConsoleMessage: OnConsoleMessage, onError: OnErrorMessage, modifyBrowserUrl: browserUrl => browserUrl + runOptions.QueryString);

        _testOutput.WriteLine("Waiting for page to load");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        if (runOptions.CheckCounter)
        {
            await page.Locator("text=Counter").ClickAsync();
            var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
            Assert.Equal("Current count: 0", txt);

            await page.Locator("text=\"Click me\"").ClickAsync();
            txt = await page.Locator("p[role='status']").InnerHTMLAsync();
            Assert.Equal("Current count: 1", txt);
        }

        if (runOptions.Test is not null)
            await runOptions.Test(page);

        _testOutput.WriteLine($"Waiting for additional 10secs to see if any errors are reported");
        await Task.Delay(10_000);

        void OnConsoleMessage(IConsoleMessage msg)
        {
            _testOutput.WriteLine($"[{msg.Type}] {msg.Text}");

            runOptions.OnConsoleMessage?.Invoke(msg);

            if (runOptions.DetectRuntimeFailures)
            {
                if (msg.Text.Contains("[MONO] * Assertion") || msg.Text.Contains("Error: [MONO] "))
                    throw new XunitException($"Detected a runtime failure at line: {msg.Text}");
            }
        }

        void OnErrorMessage(string msg)
        {
            _testOutput.WriteLine($"[ERROR] {msg}");
            runOptions.OnErrorMessage?.Invoke(msg);
        }
    }

    public string FindBlazorBinFrameworkDir(string config, bool forPublish, string framework = DefaultTargetFrameworkForBlazor)
        => _provider.FindBinFrameworkDir(config: config, forPublish: forPublish, framework: framework);
}
