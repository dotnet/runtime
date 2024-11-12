// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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
    public void WithNativeReference_AOTInProjectFile(string config)
    {
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        ProjectInfo info = CreateProjectWithNativeReference(config, aot: true, extraProperties: extraProperties);
        BlazorBuild(info, isNativeBuild: true);
        BlazorPublish(info, useCache: false, isNativeBuild: true);
        // will relink
        BlazorBuild(info, useCache: false, isNativeBuild: true);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/82725")]
    public void WithNativeReference_AOTOnCommandLine(string config)
    {
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        ProjectInfo info = CreateProjectWithNativeReference(config, aot: false, extraProperties: extraProperties);
        BlazorBuild(info, isNativeBuild: true);
        BlazorPublish(info, isNativeBuild: true, extraArgs: "-p:RunAOTCompilation=true");
        // no aot!
        BlazorPublish(info, isNativeBuild: true);
    }

    // [Theory]
    // [InlineData("Release")]
    // public void BlazorWasm_CannotAOT_WithNoTrimming(string config)
    // {
    //     string extraProperties = "<PublishTrimmed>false</PublishTrimmed><RunAOTCompilation>true</RunAOTCompilation>";
    //     ProjectInfo info = CopyTestAsset(config, aot: true, "BlazorBasicTestApp", "blazorwasm_aot", "App", extraProperties: extraProperties);

    //     bool isPublish = true;
    //     (string _, string output) = BuildTemplateProject(info,
    //         new BuildProjectOptions(
    //             info.Configuration,
    //             info.ProjectName,
    //             BinFrameworkDir: GetBlazorBinFrameworkDir(info.Configuration, isPublish),
    //             ExpectedFileType: GetExpectedFileType(info, isPublish),
    //             IsPublish: isPublish,
    //             ExpectSuccess: false)
    //     );
    //     Assert.Contains("AOT is not supported without IL trimming", output);
    // }
}
