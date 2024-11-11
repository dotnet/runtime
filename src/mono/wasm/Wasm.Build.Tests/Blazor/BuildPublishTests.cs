// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Microsoft.Playwright;
using System.Runtime.InteropServices;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class BuildPublishTests : BlazorWasmTestBase
{
    public BuildPublishTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory, TestCategory("no-workload")]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task DefaultTemplate_WithoutWorkload(string config)
    {
        ProjectInfo info = CopyTestAsset(config, aot: false, "BlazorBasicTestApp", "blz_no_workload");
        BlazorBuild(info);
        await RunForBuildWithDotnetRun(new(info.Configuration));

        BlazorPublish(info, useCache: false);
        await RunForPublishWithWebServer(new(info.Configuration));
    }


    public static TheoryData<string, bool> TestDataForDefaultTemplate_WithWorkload(bool isAot)
    {
        var data = new TheoryData<string, bool>();
        if (!isAot)
        {
            // AOT does not support managed debugging, is disabled by design
            data.Add("Debug", false);
            data.Add("Debug", true);
        }

        // [ActiveIssue("https://github.com/dotnet/runtime/issues/103625", TestPlatforms.Windows)]
        // when running locally the path might be longer than 260 chars and these tests can fail with AOT
        data.Add("Release", false); // Release relinks by default
        data.Add("Release", true);
        return data;
    }

    [Theory]
    [MemberData(nameof(TestDataForDefaultTemplate_WithWorkload), parameters: new object[] { false })]
    public void DefaultTemplate_NoAOT_WithWorkload(string config, bool testUnicode)
    {
        ProjectInfo info = CopyTestAsset(config, aot: false, "BlazorBasicTestApp", "blz_no_aot", appendUnicodeToPath: testUnicode);
        BlazorPublish(info);
    }

    [Theory]
    [MemberData(nameof(TestDataForDefaultTemplate_WithWorkload), parameters: new object[] { true })]
    public void DefaultTemplate_AOT_WithWorkload(string config, bool testUnicode)
    {
        ProjectInfo info = CopyTestAsset(config, aot: false, "BlazorBasicTestApp", "blz_aot", appendUnicodeToPath: testUnicode);
        BlazorBuild(info);

        bool isPublish = true;
        BuildTemplateProject(info,
            new BuildProjectOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBlazorBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: NativeFilesType.AOT,
                IsPublish: isPublish,
                UseCache: false),
            extraArgs: "-p:RunAOTCompilation=true"
        );
    }

    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Release", false)]
    [InlineData("Debug", true)]
    [InlineData("Release", true)]
    public void DefaultTemplate_CheckFingerprinting(string config, bool expectFingerprintOnDotnetJs)
    {
        var extraProperty = expectFingerprintOnDotnetJs ?
            "<WasmFingerprintDotnetJs>true</WasmFingerprintDotnetJs><WasmBuildNative>true</WasmBuildNative>" :
            "<WasmBuildNative>true</WasmBuildNative>";
        ProjectInfo info = CopyTestAsset(config, aot: false, "BlazorBasicTestApp", "blz_checkfingerprinting", extraProperties: extraProperty);
        BlazorBuild(info, isNativeBuild: true);
        BlazorPublish(info, isNativeBuild: true, useCache: false);
    }

    // Disabling for now - publish folder can have more than one dotnet*hash*js, and not sure
    // how to pick which one to check, for the test
    //[Theory]
    //[InlineData("Debug")]
    //[InlineData("Release")]
    //public void DefaultTemplate_AOT_OnlyWithPublishCommandLine_Then_PublishNoAOT(string config)
    //{
    //string id = $"blz_aot_pub_{config}";
    //CreateBlazorWasmTemplateProject(id);

    //// No relinking, no AOT
    //BlazorBuild(new BuildProjectOptions(id, config, NativeFilesType.FromRuntimePack);

    //// AOT=true only for the publish command line, similar to what
    //// would happen when setting it in Publish dialog for VS
    //BlazorPublish(new BuildProjectOptions(id, config, expectedFileType: NativeFilesType.AOT, "-p:RunAOTCompilation=true");

    //// publish again, no AOT
    //BlazorPublish(new BuildProjectOptions(id, config, NativeFilesType.Relinked);
    //}

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void DefaultTemplate_WithResources_Publish(string config)
    {
        string[] cultures = ["ja-JP", "es-ES"];
        ProjectInfo info = CopyTestAsset(config, aot: false, "BlazorBasicTestApp", "blz_resources");

        // Ensure we have the source data we rely on
        string resxSourcePath = Path.Combine(BuildEnvironment.TestAssetsPath, "resx");
        foreach (string culture in cultures)
            Assert.True(File.Exists(Path.Combine(resxSourcePath, $"words.{culture}.resx")));

        Utils.DirectoryCopy(resxSourcePath, Path.Combine(_projectDir!, "resx"));

        // Build and assert resource dlls
        BlazorBuild(info);
        AssertResourcesDlls(GetBlazorBinFrameworkDir(config, forPublish: false));

        // Publish and assert resource dlls
        BlazorPublish(info, useCache: false);
        AssertResourcesDlls(GetBlazorBinFrameworkDir(config, forPublish: true));

        void AssertResourcesDlls(string basePath)
        {
            foreach (string culture in cultures)
            {
                string? resourceAssemblyPath = Directory.EnumerateFiles(
                    Path.Combine(basePath, culture),
                    $"*{ProjectProviderBase.WasmAssemblyExtension}").SingleOrDefault(f => Path.GetFileNameWithoutExtension(f).StartsWith($"{info.ProjectName}.resources"));
                Assert.True(resourceAssemblyPath != null && File.Exists(resourceAssemblyPath), $"Expects to have a resource assembly at {resourceAssemblyPath}");
            }
        }
    }

    [Theory]
    [InlineData("", true)] // Default case
    [InlineData("false", false)] // the other case
    public async Task Test_WasmStripILAfterAOT(string stripILAfterAOT, bool expectILStripping)
    {
        string config = "Release";
        string extraProperties = "<RunAOTCompilation>true</RunAOTCompilation>";
        if (!string.IsNullOrEmpty(stripILAfterAOT))
            extraProperties += $"<WasmStripILAfterAOT>{stripILAfterAOT}</WasmStripILAfterAOT>";
        ProjectInfo info = CopyTestAsset(config, aot: true, "BlazorBasicTestApp", "blz_WasmStripILAfterAOT", extraProperties: extraProperties);

        BlazorPublish(info);
        await RunForPublishWithWebServer(new(config));

        string frameworkDir = Path.Combine(_projectDir!, "bin", config, BuildTestBase.DefaultTargetFrameworkForBlazor, "publish", "wwwroot", "_framework");
        string objBuildDir = Path.Combine(_projectDir!, "obj", config, BuildTestBase.DefaultTargetFrameworkForBlazor, "wasm", "for-publish");

        WasmTemplateTests.TestWasmStripILAfterAOTOutput(objBuildDir, frameworkDir, expectILStripping, _testOutput);
    }

    [Theory]
    [InlineData("Debug")]
    public void BlazorWasm_CannotAOT_InDebug(string config)
    {
        ProjectInfo info = CopyTestAsset(
            config, aot: true, "BlazorBasicTestApp", "blazorwasm", extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>");

        bool isPublish = true;
        (string _, string output) = BuildTemplateProject(info,
            new BuildProjectOptions(
                info.Configuration,
                info.ProjectName,
                BinFrameworkDir: GetBlazorBinFrameworkDir(info.Configuration, isPublish),
                ExpectedFileType: GetExpectedFileType(info, isPublish),
                IsPublish: isPublish,
                ExpectSuccess: false
        ));
        Assert.Contains("AOT is not supported in debug configuration", output);
    }
}
