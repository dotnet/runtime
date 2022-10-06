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
    [ActiveIssue("https://github.com/dotnet/runtime/issues/70985", TestPlatforms.Linux)]
    public void WithNativeReference_AOTInProjectFile(string config)
    {
        string id = $"blz_nativeref_aot_{config}_{Path.GetRandomFileName()}";
        string projectFile = CreateProjectWithNativeReference(id);
        AddItemsPropertiesToProject(projectFile, extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>");

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.AOT));

        // will relink
        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/70985", TestPlatforms.Linux)]
    public void WithNativeReference_AOTOnCommandLine(string config)
    {
        string id = $"blz_nativeref_aot_{config}_{Path.GetRandomFileName()}";
        CreateProjectWithNativeReference(id);

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.AOT), "-p:RunAOTCompilation=true");

        // no aot!
        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
    }
}



