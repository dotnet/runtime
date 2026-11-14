// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests;

public partial class LibraryInitializerTests : WasmTemplateTestsBase
{
    public LibraryInitializerTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact, TestCategory("bundler-friendly")]
    public async Task LoadLibraryInitializer()
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "LibraryInitializerTests_LoadLibraryInitializer");
        PublishProject(info, config);
        RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "LibraryInitializerTest"));
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal("LIBRARY_INITIALIZER_TEST = 1", m)
        );
    }

    [GeneratedRegex("MONO_WASM: Failed to invoke 'onRuntimeConfigLoaded' on library initializer '../WasmBasicTestApp.[a-z0-9]+.lib.module.js': Error: Error thrown from library initializer")]
    private static partial Regex AbortStartupOnErrorRegex { get; }

    [GeneratedRegex("Aborting startup, reason: Error: Failed to invoke 'onRuntimeReady' on library initializer '(\\.\\./)?WasmBasicTestApp(\\.[a-z0-9]+)?\\.lib\\.module\\.js': Error thrown from library initializer")]
    private static partial Regex AbortStartupOnErrorCoreClrRegex { get; }

    [Fact, TestCategory("bundler-friendly")]
    public async Task AbortStartupOnError()
    {
        bool isCoreClr = IsCoreClrRuntime;
        string queryParam = isCoreClr ? "throwErrorOnReady" : "throwError";
        Regex expectedRegex = isCoreClr ? AbortStartupOnErrorCoreClrRegex : AbortStartupOnErrorRegex;

        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "LibraryInitializerTests_AbortStartupOnError");
        PublishProject(info, config);

        BrowserRunOptions options = new(
            config,
            TestScenario: "LibraryInitializerTest",
            BrowserQueryString: new NameValueCollection { { queryParam, "true" } },
            ExpectedExitCode: 1);
        RunResult result = await RunForPublishWithWebServer(options);
        Assert.True(
            result.ConsoleOutput.Any(m => expectedRegex.IsMatch(m)),
            $"The library initializer test didn't emit expected error message.\nConsole output:\n{string.Join("\n", result.ConsoleOutput)}");
    }

    [Fact, TestCategory("bundler-friendly")]
    public async Task InvokeLibraryInitializersApi()
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "LibraryInitializerTests_InvokeLibraryInitializersApi");
        PublishProject(info, config);
        RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "InvokeLibraryInitializersTest"));
        Assert.Contains("customHookCalled=true", result.TestOutput);
        Assert.Contains("resources.libraryInitializers.length=1", result.TestOutput);
    }

    public static IEnumerable<object[]> AssemblyProviderConfigurations()
    {
        yield return new object[] { Configuration.Debug, true };
        yield return new object[] { Configuration.Release, true };
        yield return new object[] { Configuration.Debug, false };
    }

    [Theory, TestCategory("no-workload")]
    [MemberData(nameof(AssemblyProviderConfigurations))]
    public async Task JSExportFromAssemblyProvider(Configuration config, bool trimmed)
    {
        string useLocalTargetingPack = $"""
            <Target Name="UseLocalJSExportTargetingPack" BeforeTargets="ProcessFrameworkReferences">
                <ItemGroup>
                    <KnownFrameworkReference Update="@(KnownFrameworkReference)"
                                             Condition="'%(KnownFrameworkReference.Identity)' == 'Microsoft.NETCore.App' and '%(KnownFrameworkReference.TargetFramework)' == '{DefaultTargetFramework}'">
                        <TargetingPackVersion>{s_buildEnv.GetRuntimePackVersion(DefaultTargetFramework)}</TargetingPackVersion>
                    </KnownFrameworkReference>
                </ItemGroup>
            </Target>
            """;
        ProjectInfo info = CopyTestAsset(
            config, false, TestAsset.WasmBasicTestApp, "mapped_exports",
            extraProperties: $"<PublishTrimmed>{trimmed.ToString().ToLowerInvariant()}</PublishTrimmed><EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>",
            extraItems: $"""
                <ProjectReference Include="..\Library\Library.csproj" />
                <BlazorWebAssemblyLazyLoad Include="Library{ProjectProviderBase.WasmAssemblyExtension}" />
                """,
            insertAtEnd: useLocalTargetingPack);
        File.Copy(
            Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "JSExportProvider.cs"),
            Path.Combine(_projectDir, "..", "Library", "Library.cs"),
            overwrite: true);
        string libraryDir = Path.Combine(_projectDir, "..", "Library");
        string countingDir = Path.Combine(_projectDir, "..", "CountingLibrary");
        Directory.CreateDirectory(countingDir);
        File.Copy(Path.Combine(libraryDir, "Library.csproj"), Path.Combine(countingDir, "CountingLibrary.csproj"));
        File.Copy(
            Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "DeferredJSExportProviders.cs"),
            Path.Combine(countingDir, "DeferredJSExportProviders.cs"));
        AddItemsPropertiesToProject(Path.Combine(countingDir, "CountingLibrary.csproj"),
            extraProperties: IsCoreClrRuntime ? "" : "<DefineConstants>$(DefineConstants);TEST_MONO</DefineConstants>",
            insertAtEnd: useLocalTargetingPack);
        AddItemsPropertiesToProject(info.ProjectFilePath,
            extraItems: $"""
                <ProjectReference Include="..\CountingLibrary\CountingLibrary.csproj" />
                <BlazorWebAssemblyLazyLoad Include="CountingLibrary{ProjectProviderBase.WasmAssemblyExtension}" />
                """);
        AddItemsPropertiesToProject(
            Path.Combine(libraryDir, "Library.csproj"),
            extraProperties: "<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>",
            insertAtEnd: useLocalTargetingPack);
        PublishProject(info, config);
        string generatedFile = Directory.GetFiles(Path.Combine(libraryDir, "obj"), "JSExports.g.cs", SearchOption.AllDirectories).Single();
        string generatedCode = File.ReadAllText(generatedFile);
        Assert.Contains("file sealed class __GeneratedJSExportProvider : ", generatedCode);
        Assert.Contains("JSExportProvider>(\"Library\", typeof(", generatedCode);
        Assert.Contains("public override global::System.ReadOnlySpan<string> GetExportNames()", generatedCode);
        Assert.Contains("public override void Bind(string methodId)", generatedCode);
        Assert.DoesNotContain("__GeneratedJSExportProvider_", generatedCode);
        Assert.DoesNotContain("JSExportCatalog", generatedCode);
        Assert.DoesNotContain("ModuleInitializer", generatedCode);
        Assert.Contains("UnsafeAccessorType(", generatedCode);
        Assert.DoesNotContain("DynamicDependencyAttribute(\"__Wrapper", generatedCode);
        string appGeneratedFile = Directory.GetFiles(Path.Combine(_projectDir, "obj"), "JSExports.g.cs", SearchOption.AllDirectories).Single();
        Assert.Contains("JSExportProvider>(\"Library\")", File.ReadAllText(appGeneratedFile));
        RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DeferredJSExports"));
        Assert.Contains("Empty BCL exports passed", result.TestOutput);
        Assert.Contains("Mapped JSExports passed", result.TestOutput);
        Assert.Equal(
            new[] { "lazy:constructor", "lazy:discovered", "lazy:read", "lazy:bind", "lazy:first", "lazy:repeated" },
            result.TestOutput.Where(static message => message.StartsWith("lazy:", StringComparison.Ordinal)));
    }

    [Theory, TestCategory("no-workload")]
    [InlineData(Configuration.Debug, "net10.0", "10.0.0")]
    [InlineData(Configuration.Release, "net10.0", "10.0.0")]
    [InlineData(Configuration.Debug, "net11.0", "11.0.0-rc.1.26425.128")]
    [InlineData(Configuration.Release, "net11.0", "11.0.0-rc.1.26425.128")]
    public async Task JSExportFromLegacyLibrary(Configuration config, string targetFramework, string targetingPackVersion)
    {
        ProjectInfo info = CopyTestAsset(
            config, false, TestAsset.WasmBasicTestApp, "legacy_exports",
            extraProperties: "<PublishTrimmed>true</PublishTrimmed>",
            extraItems: $"""
                <Reference Include="LegacyExports">
                    <HintPath>..\LegacyExports\bin\{config}\{targetFramework}\LegacyExports.dll</HintPath>
                </Reference>
                <TrimmerRootAssembly Include="LegacyExports" />
                """);
        string libraryDir = Path.Combine(_projectDir, "..", "LegacyExports");
        Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "LegacyExports"), libraryDir);

        // Do not let the parent WBT overrides substitute the in-tree targeting pack or generator.
        using (ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
            .WithWorkingDirectory(libraryDir)
            .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir))
        {
            cmd.ExecuteWithCapturedOutput(
                $"build -c {config} -bl:\"{Path.Combine(_logPath, "LegacyExports.binlog")}\"",
                $"-p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false -p:LegacyTargetFramework={targetFramework} -p:LegacyTargetingPackVersion={targetingPackVersion}")
                .EnsureSuccessful();
        }

        string generatedFile = Directory.GetFiles(Path.Combine(libraryDir, "obj"), "JSExports.g.cs", SearchOption.AllDirectories).Single();
        string generatedCode = File.ReadAllText(generatedFile);
        Assert.Contains("JSFunctionBinding.BindManagedFunction(", generatedCode);
        Assert.Contains("static void __Register_()", generatedCode);
        Assert.DoesNotContain("RegisterAssemblyExports", generatedCode);
        Assert.DoesNotContain("JSExportProvider", generatedCode);

        string libraryPath = Path.Combine(libraryDir, "bin", config.ToString(), targetFramework, "LegacyExports.dll");
        byte[] libraryBytes = File.ReadAllBytes(libraryPath);
        PublishProject(info, config);
        Assert.Equal(libraryBytes, File.ReadAllBytes(libraryPath));

        RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "LegacyJSExports"));
        Assert.Contains("Legacy JSExports passed", result.TestOutput);
    }
}
