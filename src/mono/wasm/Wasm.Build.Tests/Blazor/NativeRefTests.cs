// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class NativeTests : BlazorWasmTestBase
{
    public NativeTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/82725")]
    public async Task WithNativeReference_AOTInProjectFileAsync(string config)
    {
        string id = $"blz_nativeref_aot_{config}_{GetRandomId()}";
        string projectFile = await CreateProjectWithNativeReferenceAsync(id);
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        AddItemsPropertiesToProject(projectFile, extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>" + extraProperties);

        await BlazorBuildAsync(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        await BlazorPublishAsync(new BlazorBuildOptions(id, config, NativeFilesType.AOT, ExpectRelinkDirWhenPublishing: true));

        // will relink
        await BlazorBuildAsync(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/82725")]
    public async Task WithNativeReference_AOTOnCommandLineAsync(string config)
    {
        string id = $"blz_nativeref_aot_{config}_{GetRandomId()}";
        string projectFile = await CreateProjectWithNativeReferenceAsync(id);
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

        await BlazorBuildAsync(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        await BlazorPublishAsync(new BlazorBuildOptions(id, config, NativeFilesType.AOT, ExpectRelinkDirWhenPublishing: true), "-p:RunAOTCompilation=true");

        // no aot!
        await BlazorPublishAsync(new BlazorBuildOptions(id, config, NativeFilesType.Relinked, ExpectRelinkDirWhenPublishing: true));
    }

    [Theory]
    [InlineData("Release")]
    public async Task BlazorWasm_CannotAOT_WithNoTrimmingAsync(string config)
    {
        string id = $"blazorwasm_{config}_aot_{GetRandomId()}";
        await CreateBlazorWasmTemplateProjectAsync(id);
        AddItemsPropertiesToProject(Path.Combine(_projectDir!, $"{id}.csproj"),
                                    extraItems: null,
                                    extraProperties: "<PublishTrimmed>false</PublishTrimmed><RunAOTCompilation>true</RunAOTCompilation>");

        (CommandResult res, _) = await BlazorPublishAsync(new BlazorBuildOptions(id, config, ExpectSuccess: false));
        Assert.Contains("AOT is not supported without IL trimming", res.Output);
    }
}
