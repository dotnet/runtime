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
    protected BlazorWasmTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(output, buildContext, new WasmSdkBasedProjectProvider(output, DefaultTargetFrameworkForBlazor))
    {
        _provider = GetProvider<WasmSdkBasedProjectProvider>();
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
        };

    public override (string projectDir, string buildOutput) BuildProject(
        ProjectInfo projectInfo,
        BuildOptions buildOptions,
        params string[] extraArgs)
    {
        try
        {
            var additionalOptiont = buildOptions.WarnAsError
                ? new[] { "-p:BlazorEnableCompression=false", "/warnaserror" }
                : new[] { "-p:BlazorEnableCompression=false" };
    
            (string projectDir, string buildOutput) = base.BuildProject(
                projectInfo,
                buildOptions with { AssertAppBundle = false },
                extraArgs.Concat(additionalOptiont).ToArray());

            if (buildOptions.ExpectSuccess && buildOptions.AssertAppBundle)
            {
                AssertBundle(buildOutput, buildOptions);
            }

            return (projectDir, buildOutput);
        }
        catch (XunitException xe)
        {
            if (xe.Message.Contains("error CS1001: Identifier expected"))
                Utils.DirectoryCopy(_projectDir!, _logPath, testOutput: _testOutput);
            throw;
        }
    }

    protected void UpdateHomePage() =>
        UpdateFile(Path.Combine("Pages", "Home.razor"), blazorHomePageReplacements);

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

    protected (string projectDir, string buildOutput) BlazorBuild(
        ProjectInfo info, bool isNativeBuild = false, bool useCache = true, params string[] extraArgs)
    {
        bool isPublish = false;
        return BuildProject(info,
            new BuildOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBlazorBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild),
                IsPublish: isPublish,
                UseCache: useCache),
            extraArgs
        );
    }

    protected (string projectDir, string buildOutput) BlazorPublish(
        ProjectInfo info, bool isNativeBuild = false, bool useCache = true, params string[] extraArgs)
    {
        bool isPublish = true;
        return BuildProject(info,
            new BuildOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBlazorBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild),
                IsPublish: isPublish,
                UseCache: useCache),
            extraArgs
        );
    }

    public void AssertBundle(string buildOutput, BuildOptions buildOptions)
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

    protected ProjectInfo CreateProjectWithNativeReference(string config, bool aot, string extraProperties)
    {
        string extraItems = @$"
            {GetSkiaSharpReferenceItems()}
            <WasmFilesToIncludeInFileSystem Include=""{Path.Combine(BuildEnvironment.TestAssetsPath, "mono.png")}"" />
        ";
        return CopyTestAsset(
            config, aot, "BlazorBasicTestApp", "blz_nativeref_aot", "App", extraItems: extraItems, extraProperties: extraProperties);
    }

    // Keeping these methods with explicit Build/Publish in the name
    // so in the test code it is evident which is being run!
    public override async Task<RunResult> RunForBuildWithDotnetRun(RunOptions runOptions)
        => await base.RunForBuildWithDotnetRun(runOptions with {
            ExecuteAfterLoaded = runOptions.ExecuteAfterLoaded ?? _executeAfterLoaded
        });

    public override async Task<RunResult> RunForPublishWithWebServer(RunOptions runOptions)
        => await base.RunForPublishWithWebServer(runOptions with {
            ExecuteAfterLoaded = runOptions.ExecuteAfterLoaded ?? _executeAfterLoaded
        });

    public string GetBlazorBinFrameworkDir(string config, bool forPublish, string framework = DefaultTargetFrameworkForBlazor, string? projectDir = null)
        => _provider.GetBinFrameworkDir(config: config, forPublish: forPublish, framework: framework, projectDir: projectDir);
}
