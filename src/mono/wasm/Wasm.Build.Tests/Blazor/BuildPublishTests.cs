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
        string id = $"blz_no_workload_{config}_{GetRandomId()}_{s_unicodeChar}";
        CreateBlazorWasmTemplateProject(id);

        BlazorBuild(new BlazorBuildOptions(id, config));
        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });

        BlazorPublish(new BlazorBuildOptions(id, config));
        await BlazorRunForPublishWithWebServer(new BlazorRunOptions() { Config = config });
    }


    public static TheoryData<string, bool> TestDataForDefaultTemplate_WithWorkload(bool isAot)
    {
        var data = new TheoryData<string, bool>();
        data.Add("Debug", false);
        data.Add("Release", false); // Release relinks by default
        // [ActiveIssue("https://github.com/dotnet/runtime/issues/83497", TestPlatforms.Windows)]
        if (!isAot || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            data.Add("Debug", true); // for aot:true on Windows, it fails
        }

        // [ActiveIssue("https://github.com/dotnet/runtime/issues/83497", TestPlatforms.Windows)]
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            data.Add("Release", true);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(TestDataForDefaultTemplate_WithWorkload), parameters: new object[] { false })]
    public void DefaultTemplate_NoAOT_WithWorkload(string config, bool testUnicode)
    {
        string id = testUnicode ?
            $"blz_no_aot_{config}_{GetRandomId()}_{s_unicodeChar}" :
            $"blz_no_aot_{config}_{GetRandomId()}";
        CreateBlazorWasmTemplateProject(id);

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));
        if (config == "Release")
        {
            // relinking in publish for Release config
            BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked, ExpectRelinkDirWhenPublishing: true));
        }
        else
        {
            BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack, ExpectRelinkDirWhenPublishing: true));
        }
    }

    [Theory]
    [MemberData(nameof(TestDataForDefaultTemplate_WithWorkload), parameters: new object[] { true })]
    public void DefaultTemplate_AOT_WithWorkload(string config, bool testUnicode)
    {
        string id = testUnicode ?
            $"blz_aot_{config}_{GetRandomId()}_{s_unicodeChar}" :
            $"blz_aot_{config}_{GetRandomId()}";
        CreateBlazorWasmTemplateProject(id);

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));
        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.AOT), "-p:RunAOTCompilation=true");
    }

    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Release", false)]
    [InlineData("Debug", true)]
    [InlineData("Release", true)]
    public void DefaultTemplate_CheckFingerprinting(string config, bool expectFingerprintOnDotnetJs)
    {
        string id = $"blz_checkfingerprinting_{config}_{GetRandomId()}";

        CreateBlazorWasmTemplateProject(id);

        var options = new BlazorBuildOptions(id, config, NativeFilesType.Relinked, ExpectRelinkDirWhenPublishing: true, ExpectFingerprintOnDotnetJs: expectFingerprintOnDotnetJs);
        var finterprintingArg = expectFingerprintOnDotnetJs ? "/p:WasmFingerprintDotnetJs=true" : string.Empty;

        BlazorBuild(options, "/p:WasmBuildNative=true", finterprintingArg);
        BlazorPublish(options, "/p:WasmBuildNative=true", finterprintingArg);
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
    //BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack);

    //// AOT=true only for the publish command line, similar to what
    //// would happen when setting it in Publish dialog for VS
    //BlazorPublish(new BlazorBuildOptions(id, config, expectedFileType: NativeFilesType.AOT, "-p:RunAOTCompilation=true");

    //// publish again, no AOT
    //BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked);
    //}

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void DefaultTemplate_WithResources_Publish(string config)
    {
        string[] cultures = ["ja-JP", "es-ES"];
        string id = $"blz_resources_{config}_{GetRandomId()}";
        CreateBlazorWasmTemplateProject(id);

        // Ensure we have the source data we rely on
        string resxSourcePath = Path.Combine(BuildEnvironment.TestAssetsPath, "resx");
        foreach (string culture in cultures)
            Assert.True(File.Exists(Path.Combine(resxSourcePath, $"words.{culture}.resx")));

        Utils.DirectoryCopy(resxSourcePath, Path.Combine(_projectDir!, "resx"));

        // Build and assert resource dlls
        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));
        AssertResourcesDlls(FindBlazorBinFrameworkDir(config, false));

        // Publish and assert resource dlls
        if (config == "Release")
        {
            // relinking in publish for Release config
            BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked, ExpectRelinkDirWhenPublishing: true));
        }
        else
        {
            BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack, ExpectRelinkDirWhenPublishing: true));
        }

        AssertResourcesDlls(FindBlazorBinFrameworkDir(config, true));

        void AssertResourcesDlls(string basePath)
        {
            foreach (string culture in cultures)
            {
                string resourceAssemblyPath = Path.Combine(basePath, culture, $"{id}.resources{ProjectProviderBase.WasmAssemblyExtension}");
                Assert.True(File.Exists(resourceAssemblyPath), $"Expects to have a resource assembly at {resourceAssemblyPath}");
            }
        }
    }

    [Fact]
    public void Test_WasmStripILAfterAOT()
    {
        string id = $"blz_test_WasmStripILAfterAOT_Release_{GetRandomId()}";
        string projectFile = CreateBlazorWasmTemplateProject(id);
        string projectDirectory = Path.GetDirectoryName(projectFile)!;

        BlazorBuild(new BlazorBuildOptions(id, "Release", NativeFilesType.FromRuntimePack));
        BlazorPublish(new BlazorBuildOptions(id, "Release", NativeFilesType.AOT, AssertAppBundle : false), "-p:RunAOTCompilation=true -p:WasmStripILAfterAOT=true -p:WasmEnableWebcil=false");

        string frameworkDir = Path.Combine(projectDirectory, "bin", "Release", BuildTestBase.DefaultTargetFramework, "publish", "wwwroot", "_framework");
        string objBuildDir = Path.Combine(projectDirectory, "obj", "Release", BuildTestBase.DefaultTargetFramework, "wasm", "for-publish");
        string origAssemblyDir = Path.Combine(objBuildDir, "aot-in");
        string strippedAssemblyDir = Path.Combine(objBuildDir, "stripped");
        Assert.True(Directory.Exists(origAssemblyDir), $"Could not find the original AOT input assemblies dir: {origAssemblyDir}");
        Assert.True(Directory.Exists(strippedAssemblyDir), $"Could not find the stripped assemblies dir: {strippedAssemblyDir}");

        string assemblyToExamine = "System.Private.CoreLib.dll";
        string originalAssembly = Path.Combine(objBuildDir, origAssemblyDir, assemblyToExamine);
        string strippedAssembly = Path.Combine(objBuildDir, strippedAssemblyDir, assemblyToExamine);
        string bundledAssembly = Path.Combine(frameworkDir, assemblyToExamine);
        Assert.True(File.Exists(originalAssembly));
        Assert.True(File.Exists(strippedAssembly));
        Assert.True(File.Exists(bundledAssembly));

        string compressedOriginalAssembly = Compress(originalAssembly);
        string compressedStrippedAssembly = Compress(strippedAssembly);
        string compressedBundledAssembly = bundledAssembly + ".gz";
        FileInfo compressedOriginalAssembly_fi = new FileInfo(compressedOriginalAssembly);
        FileInfo compressedStrippedAssembly_fi = new FileInfo(compressedStrippedAssembly);
        FileInfo compressedBundledAssembly_fi = new FileInfo(compressedBundledAssembly);
        Assert.True(compressedOriginalAssembly_fi.Length > compressedBundledAssembly_fi.Length);
        Assert.Equal(compressedBundledAssembly_fi.Length, compressedStrippedAssembly_fi.Length);
    }
}
