// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class MiscTests : BuildTestBase
{
    public MiscTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory]
    [InlineData("Debug", true)]
    [InlineData("Debug", false)]
    [InlineData("Release", true)]
    [InlineData("Release", false)]
    public void NativeBuild_WithDeployOnBuild_UsedByVS(string config, bool nativeRelink)
    {
        string id = $"blz_deploy_on_build_{config}_{nativeRelink}_{Path.GetRandomFileName()}";
        string projectFile = CreateProjectWithNativeReference(id);
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        if (!nativeRelink)
            extraProperties += "<RunAOTCompilation>true</RunAOTCompilation>";
        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

        // build with -p:DeployOnBuild=true, and that will trigger a publish
        (CommandResult res, _) = BlazorBuildInternal(id, config, publish: false, setWasmDevel: false, "-p:DeployOnBuild=true");

        var expectedFileType = nativeRelink ? NativeFilesType.Relinked : NativeFilesType.AOT;

        AssertDotNetNativeFiles(expectedFileType, config, forPublish: true, targetFramework: DefaultTargetFrameworkForBlazor);
        AssertBlazorBundle(config, isPublish: true, dotnetWasmFromRuntimePack: false);

        if (expectedFileType == NativeFilesType.AOT)
        {
            // check for this too, so we know the format is correct for the negative
            // test for jsinterop.webassembly.dll
            Assert.Contains("Microsoft.JSInterop.dll -> Microsoft.JSInterop.dll.bc", res.Output);

            // make sure this assembly gets skipped
            Assert.DoesNotContain("Microsoft.JSInterop.WebAssembly.dll -> Microsoft.JSInterop.WebAssembly.dll.bc", res.Output);
        }

        // Check that we linked only for publish
        string objBuildDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFramework, "wasm", "for-build");
        Assert.False(Directory.Exists(objBuildDir), $"Found unexpected {objBuildDir}, which gets creating when relinking during Build");

        // double check!
        int index = res.Output.IndexOf("pinvoke.c -> pinvoke.o");
        Assert.NotEqual(-1, index);

        // there should be only one instance of this string!
        index = res.Output.IndexOf("pinvoke.c -> pinvoke.o", index + 1);
        Assert.Equal(-1, index);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void DefaultTemplate_AOT_InProjectFile(string config)
    {
        string id = $"blz_aot_prj_file_{config}_{Path.GetRandomFileName()}";
        string projectFile = CreateBlazorWasmTemplateProject(id);

        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        AddItemsPropertiesToProject(projectFile, extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>" + extraProperties);

        // No relinking, no AOT
        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));

        // will aot
        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.AOT, ExpectRelinkDirWhenPublishing: true));

        // build again
        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));
    }
}
