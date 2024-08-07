// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class MiscTests : BlazorWasmTestBase
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
    [ActiveIssue("https://github.com/dotnet/runtime/issues/103566")]
    public void NativeBuild_WithDeployOnBuild_UsedByVS(string config, bool nativeRelink)
    {
        string id = $"blz_deploy_on_build_{config}_{nativeRelink}_{GetRandomId()}";
        string projectFile = CreateProjectWithNativeReference(id);
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        if (!nativeRelink)
            extraProperties += "<RunAOTCompilation>true</RunAOTCompilation>";
        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

        // build with -p:DeployOnBuild=true, and that will trigger a publish
        (CommandResult res, _) = BlazorBuild(new BlazorBuildOptions(
                                        Id: id,
                                        Config: config,
                                        ExpectedFileType: nativeRelink ? NativeFilesType.Relinked : NativeFilesType.AOT,
                                        ExpectRelinkDirWhenPublishing: false,
                                        IsPublish: false),
                                    "-p:DeployBuild=true");

        // double check relinking!
        int index = res.Output.IndexOf("pinvoke.c -> pinvoke.o");
        Assert.NotEqual(-1, index);

        // there should be only one instance of this string!
        index = res.Output.IndexOf("pinvoke.c -> pinvoke.o", index + 1);
        Assert.Equal(-1, index);
    }

    [Theory]
    [InlineData("Release")]
    public void DefaultTemplate_AOT_InProjectFile(string config)
    {
        string id = $"blz_aot_prj_file_{config}_{GetRandomId()}";
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
