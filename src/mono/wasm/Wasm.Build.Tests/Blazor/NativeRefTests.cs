// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

// TODO-WASM: https://github.com/dotnet/runtime/issues/135046
[TestCategory("native"), TestCategory("mono")]
public class NativeTests : BlazorWasmTestBase
{
    public NativeTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/82725")]
    public void WithNativeReference_AOTInProjectFile(Configuration config)
    {
        string extraProperties = config == Configuration.Debug
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>" +
                                        "<RunAOTCompilation>true</RunAOTCompilation>")
                                    : "<RunAOTCompilation>true</RunAOTCompilation>";
        ProjectInfo info = CreateProjectWithNativeReference(config, aot: true, extraProperties: extraProperties);
        BlazorBuild(info, config, isNativeBuild: true);
        BlazorPublish(info, config, new PublishOptions(UseCache: false), isNativeBuild: true);
        // will relink
        BlazorBuild(info, config, new BuildOptions(UseCache: false), isNativeBuild: true);
    }

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/82725")]
    public void WithNativeReference_AOTOnCommandLine(Configuration config)
    {
        string extraProperties = config == Configuration.Debug
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        ProjectInfo info = CreateProjectWithNativeReference(config, aot: false, extraProperties: extraProperties);
        BlazorBuild(info, config, isNativeBuild: true);
        BlazorPublish(info, config, new PublishOptions(AOT: true), isNativeBuild: true);
        // no aot!
        BlazorPublish(info, config, isNativeBuild: true);
    }
}
