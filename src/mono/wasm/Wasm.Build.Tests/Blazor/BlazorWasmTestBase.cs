// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
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
    private readonly string _blazorExtraMSBuildArgs = "/warnaserror";
    protected readonly PublishOptions _defaultBlazorPublishOptions;
    private readonly BuildOptions _defaultBlazorBuildOptions;

    protected BlazorWasmTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(output, buildContext, new WasmSdkBasedProjectProvider(output, DefaultTargetFrameworkForBlazor))
    {
        _provider = GetProvider<WasmSdkBasedProjectProvider>();
        _defaultBlazorPublishOptions = _defaultPublishOptions with { ExtraMSBuildArgs = _blazorExtraMSBuildArgs };
        _defaultBlazorBuildOptions = _defaultBuildOptions with { ExtraMSBuildArgs = _blazorExtraMSBuildArgs };
    }

    private Dictionary<string, string> blazorHomePageReplacements = new Dictionary<string, string>
        {
            {
                "Welcome to your new app.",
                """
                Welcome to your new app.
                @code {
                    protected override void OnAfterRender(bool firstRender)
                    {
                        if (firstRender)
                        {
                            Console.WriteLine("WASM EXIT 0");
                        }
                    }
                }
                """ }
        };

    private Func<RunOptions, IPage, Task>? _executeAfterLoaded = async (runOptions, page) =>
        {
            if (runOptions is BlazorRunOptions bro && bro.CheckCounter)
            {
                await page.Locator("text=Counter").ClickAsync();
                var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
                Assert.Equal("Current count: 0", txt);

                await page.Locator("text=\"Click me\"").ClickAsync();
                await Task.Delay(300);
                txt = await page.Locator("p[role='status']").InnerHTMLAsync();
                Assert.Equal("Current count: 1", txt);
            }
        };

    protected void UpdateHomePage() =>
        UpdateFile(Path.Combine("Pages", "Home.razor"), blazorHomePageReplacements);

    public void InitBlazorWasmProjectDir(string id, string? targetFramework = null)
    {
        targetFramework ??= DefaultTargetFrameworkForBlazor;
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
        CommandResult result = dotnetCommand.WithWorkingDirectory(_projectDir)
            .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
            .ExecuteWithCapturedOutput("new blazorwasm")
            .EnsureSuccessful();

        return Path.Combine(_projectDir, $"{id}.csproj");
    }

    protected (string projectDir, string buildOutput) BlazorBuild(ProjectInfo info, Configuration config, bool? isNativeBuild = null) =>
        BlazorBuild(info, config, _defaultBlazorBuildOptions, isNativeBuild);

    protected (string projectDir, string buildOutput) BlazorBuild(
        ProjectInfo info, Configuration config, BuildOptions buildOptions, bool? isNativeBuild = null)
    {
        try
        {
            if (buildOptions != _defaultBlazorBuildOptions)
                buildOptions = buildOptions with { ExtraMSBuildArgs = $"{_blazorExtraMSBuildArgs} {buildOptions.ExtraMSBuildArgs}" };
            (string projectDir, string buildOutput) = BuildProject(
                info,
                config,
                buildOptions,
                isNativeBuild);
            if (buildOptions.ExpectSuccess && buildOptions.AssertAppBundle)
            {
                // additional blazor-only assert, basic assert is done in BuildProject
                AssertBundle(config, buildOutput, buildOptions, isNativeBuild);
            }
            return (projectDir, buildOutput);
        }
        catch (XunitException xe)
        {
            if (xe.Message.Contains("error CS1001: Identifier expected"))
                Utils.DirectoryCopy(_projectDir, _logPath, testOutput: _testOutput);
            throw;
        }
    }

    protected (string projectDir, string buildOutput) BlazorPublish(ProjectInfo info, Configuration config, bool? isNativeBuild = null) =>
        BlazorPublish(info, config, _defaultBlazorPublishOptions, isNativeBuild);

    protected (string projectDir, string buildOutput) BlazorPublish(
        ProjectInfo info, Configuration config, PublishOptions publishOptions, bool? isNativeBuild = null)
    {
        try
        {
            if (publishOptions != _defaultBlazorPublishOptions)
                publishOptions = publishOptions with { ExtraMSBuildArgs = $"{_blazorExtraMSBuildArgs} {publishOptions.ExtraMSBuildArgs}" };
            (string projectDir, string buildOutput) = PublishProject(
                info,
                config,
                publishOptions,
                isNativeBuild);
            if (publishOptions.ExpectSuccess && publishOptions.AssertAppBundle)
            {
                // additional blazor-only assert, basic assert is done in PublishProject
                AssertBundle(config, buildOutput, publishOptions, isNativeBuild);
            }
            return (projectDir, buildOutput);
        }
        catch (XunitException xe)
        {
            if (xe.Message.Contains("error CS1001: Identifier expected"))
                Utils.DirectoryCopy(_projectDir, _logPath, testOutput: _testOutput);
            throw;
        }
    }

    public void AssertBundle(Configuration config, string buildOutput, MSBuildOptions buildOptions, bool? isNativeBuild = null)
    {
        if (!buildOptions.IsPublish)
            return;

        var expectedFileType = _provider.GetExpectedFileType(config, buildOptions.AOT, buildOptions.IsPublish, IsUsingWorkloads, isNativeBuild);
        // Publish specific checks
        if (expectedFileType == NativeFilesType.AOT)
        {
            // check for this too, so we know the format is correct for the negative
            // test for jsinterop.webassembly.dll
            Assert.Contains("Microsoft.JSInterop.dll -> Microsoft.JSInterop.dll.bc", buildOutput);

            // make sure this assembly gets skipped
            Assert.DoesNotContain("Microsoft.JSInterop.WebAssembly.dll -> Microsoft.JSInterop.WebAssembly.dll.bc", buildOutput);
        }

        string objBuildDir = Path.Combine(_projectDir, "obj", config.ToString(), buildOptions.TargetFramework!, "wasm", "for-build");
        // Check that we linked only for publish
        if (buildOptions is PublishOptions publishOptions && publishOptions.ExpectRelinkDirWhenPublishing)
            Assert.True(Directory.Exists(objBuildDir), $"Could not find expected {objBuildDir}, which gets created when relinking during Build. This is likely a test authoring error");
        else
            Assert.False(File.Exists(Path.Combine(objBuildDir, "emcc-link.rsp")), $"Found unexpected `emcc-link.rsp` in {objBuildDir}, which gets created when relinking during Build.");
    }

    protected ProjectInfo CreateProjectWithNativeReference(Configuration config, bool aot, string extraProperties)
    {
        string extraItems = @$"
            {GetSkiaSharpReferenceItems()}
            <WasmFilesToIncludeInFileSystem Include=""{Path.Combine(BuildEnvironment.TestAssetsPath, "mono.png")}"" />
        ";
        return CopyTestAsset(
            config, aot, TestAsset.BlazorBasicTestApp, "blz_nativeref_aot", extraItems: extraItems, extraProperties: extraProperties);
    }

    // Keeping these methods with explicit Build/Publish in the name
    // so in the test code it is evident which is being run!
    public override async Task<RunResult> RunForBuildWithDotnetRun(RunOptions runOptions) =>
        await base.RunForBuildWithDotnetRun(runOptions with {
            ExecuteAfterLoaded = runOptions.ExecuteAfterLoaded ?? _executeAfterLoaded,
            ServerEnvironment = GetServerEnvironmentForBuild(runOptions.ServerEnvironment)
        });

    public override async Task<RunResult> RunForPublishWithWebServer(RunOptions runOptions)
        => await base.RunForPublishWithWebServer(runOptions with {
            ExecuteAfterLoaded = runOptions.ExecuteAfterLoaded ?? _executeAfterLoaded
        });

    private Dictionary<string, string>? GetServerEnvironmentForBuild(Dictionary<string, string>? originalServerEnv)
    {
        var serverEnvironment = new Dictionary<string, string>();
        if (originalServerEnv != null)
        {
            foreach (var kvp in originalServerEnv)
            {
                serverEnvironment.Add(kvp.Key, kvp.Value);
            }
        }
        // avoid "System.IO.IOException: address already in use"
        serverEnvironment.Add("ASPNETCORE_URLS", "http://127.0.0.1:0");
        return serverEnvironment;
    }

    public string GetBlazorBinFrameworkDir(Configuration config, bool forPublish, string? framework = null, string? projectDir = null)
        => _provider.GetBinFrameworkDir(config: config, forPublish: forPublish, framework: framework ?? DefaultTargetFrameworkForBlazor, projectDir: projectDir);
}
