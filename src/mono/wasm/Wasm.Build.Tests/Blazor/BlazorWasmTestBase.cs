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

public abstract class BlazorWasmTestBase : WasmTemplateTestsBase
{
    protected readonly WasmSdkBasedProjectProvider _provider;
    protected BlazorWasmTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(output, buildContext, new WasmSdkBasedProjectProvider(output, DefaultTargetFrameworkForBlazor))
    {
        _provider = GetProvider<WasmSdkBasedProjectProvider>();
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
        using DotNetCommand dotnetCommand = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false);
        CommandResult result = dotnetCommand.WithWorkingDirectory(_projectDir!)
            .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
            .ExecuteWithCapturedOutput("new blazorwasm")
            .EnsureSuccessful();

        return Path.Combine(_projectDir!, $"{id}.csproj");
    }

    protected (CommandResult, string) BlazorBuild(BuildProjectOptions options, params string[] extraArgs)
    {
        if (options.WarnAsError)
            extraArgs = extraArgs.Append("/warnaserror").ToArray();

        (CommandResult res, string logPath) = BlazorBuildInternal(
            options.Id,
            options.Configuration,
            setWasmDevel: false,
            expectSuccess: options.ExpectSuccess,
            extraArgs: extraArgs);

        if (options.ExpectSuccess && options.AssertAppBundle)
        {
            AssertBundle(res.Output, options with { IsPublish = false });
        }

        return (res, logPath);
    }

    protected (CommandResult, string) BlazorPublish(BuildProjectOptions options, params string[] extraArgs)
    {
        if (options.WarnAsError)
            extraArgs = extraArgs.Append("/warnaserror").ToArray();

        (CommandResult res, string logPath) = BlazorBuildInternal(options.Id, options.Configuration, publish: true, setWasmDevel: false, expectSuccess: options.ExpectSuccess, extraArgs);

        if (options.ExpectSuccess && options.AssertAppBundle)
        {
            // Because we do relink in Release publish by default
            if (options.Configuration == "Release")
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
                        new BuildProjectOptions(
                            id,
                            config,
                            GetBlazorBinFrameworkDir(config, forPublish: publish),
                            UseCache: false,
                            IsPublish: publish,
                            ExpectSuccess: expectSuccess),
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

    public void AssertBundle(string buildOutput, BuildProjectOptions buildOptions)
    {
        if (IsUsingWorkloads)
        {
            // In no-workload case, the path would be from a restored nuget
            ProjectProviderBase.AssertRuntimePackPath(buildOutput, buildOptions.TargetFramework ?? DefaultTargetFramework, buildOptions.RuntimeType);
        }

        _provider.AssertBundle(buildOptions);

        if (!buildOptions.IsPublish)
            return;

        // Publish specific checks

        if (buildOptions.ExpectedFileType == NativeFilesType.AOT)
        {
            // check for this too, so we know the format is correct for the negative
            // test for jsinterop.webassembly.dll
            Assert.Contains("Microsoft.JSInterop.dll -> Microsoft.JSInterop.dll.bc", buildOutput);

            // make sure this assembly gets skipped
            Assert.DoesNotContain("Microsoft.JSInterop.WebAssembly.dll -> Microsoft.JSInterop.WebAssembly.dll.bc", buildOutput);
        }

        string objBuildDir = Path.Combine(_projectDir!, "obj", buildOptions.Configuration, buildOptions.TargetFramework!, "wasm", "for-build");
        // Check that we linked only for publish
        if (buildOptions.ExpectRelinkDirWhenPublishing)
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
    public async Task<string> BlazorRunForBuildWithDotnetRun(RunOptions runOptions)
        => await BlazorRunTest(runOptions with { Host = RunHost.DotnetRun });

    public async Task<string> BlazorRunForPublishWithWebServer(RunOptions runOptions)
        => await BlazorRunTest(runOptions with { Host = RunHost.WebServer });

    public async Task<string> BlazorRunTest(RunOptions runOptions)
    {
        if (runOptions.ExecuteAfterLoaded is null)
        {
            runOptions = runOptions with { ExecuteAfterLoaded = async (runOptions, page) =>
                {
                    if (runOptions.CheckCounter)
                    {
                        await page.Locator("text=Counter").ClickAsync();
                        var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
                        Assert.Equal("Current count: 0", txt);

                        await page.Locator("text=\"Click me\"").ClickAsync();
                        await Task.Delay(300);
                        txt = await page.Locator("p[role='status']").InnerHTMLAsync();
                        Assert.Equal("Current count: 1", txt);
                    }
                }
            };
        }
        switch (runOptions.Host)
        {
            case RunHost.DotnetRun:
                return await BrowserRunTest($"run -c {runOptions.Configuration} --no-build", _projectDir!, runOptions);
            case RunHost.WebServer:
                return await BrowserRunTest($"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files",
                    Path.GetFullPath(Path.Combine(GetBlazorBinFrameworkDir(runOptions.Configuration, forPublish: true), "..")),
                    runOptions);
            default:
                throw new NotImplementedException(runOptions.Host.ToString());
        }
    }

    public string GetBlazorBinFrameworkDir(string config, bool forPublish, string framework = DefaultTargetFrameworkForBlazor, string? projectDir = null)
        => _provider.GetBinFrameworkDir(config: config, forPublish: forPublish, framework: framework, projectDir: projectDir);
}
