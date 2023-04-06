// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class NativeRefTests : BuildTestBase
{
    public NativeRefTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/82725")]
    public void WithNativeReference_AOTInProjectFile(string config)
    {
        string id = $"blz_nativeref_aot_{config}_{Path.GetRandomFileName()}";
        string projectFile = CreateProjectWithNativeReference(id);
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        AddItemsPropertiesToProject(projectFile, extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>" + extraProperties);

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.AOT, ExpectRelinkDirWhenPublishing: true));

        // will relink
        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/82725")]
    public void WithNativeReference_AOTOnCommandLine(string config)
    {
        string id = $"blz_nativeref_aot_{config}_{Path.GetRandomFileName()}";
        string projectFile = CreateProjectWithNativeReference(id);
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.AOT, ExpectRelinkDirWhenPublishing: true), "-p:RunAOTCompilation=true");

        // no aot!
        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked, ExpectRelinkDirWhenPublishing: true));
    }
}



