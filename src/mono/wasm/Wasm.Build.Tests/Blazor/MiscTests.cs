// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        if (!nativeRelink)
            extraProperties += "<RunAOTCompilation>true</RunAOTCompilation>";
        ProjectInfo info = CopyTestAsset(config, aot: true, BasicTestApp, "blz_deploy_on_build", extraProperties: extraProperties);

        // build with -p:DeployOnBuild=true, and that will trigger a publish
        (string _, string buildOutput) = BlazorBuild(info, isNativeBuild: true, extraArgs: "-p:DeployBuild=true");

        // double check relinking!
        string substring = "pinvoke.c -> pinvoke.o";
        Assert.Contains(substring, buildOutput);

        // there should be only one instance of this string!
        int occurrences = buildOutput.Split(new[] { substring }, StringSplitOptions.None).Length - 1;
        Assert.Equal(2, occurrences);
    }

    [Theory]
    [InlineData("Release")]
    public void DefaultTemplate_AOT_InProjectFile(string config)
    {
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        ProjectInfo info = CopyTestAsset(config, aot: true, BasicTestApp, "blz_aot_prj_file", extraProperties: extraProperties);

        // No relinking, no AOT
        BlazorBuild(info);

        // will aot
        BlazorPublish(info, useCache: false);

        // build again
        BlazorBuild(info, useCache: false);
    }
}
