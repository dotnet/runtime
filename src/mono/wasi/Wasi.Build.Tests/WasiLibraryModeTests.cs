// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Wasm.Build.Tests;

#nullable enable

namespace Wasi.Build.Tests;

public class WasiLibraryModeTests : BuildTestBase
{
    public WasiLibraryModeTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Microsoft.NET.Sdk", false)]
    [InlineData("Microsoft.NET.Sdk.WebAssembly", true)]
    public void LibraryModeBuild(string sdk, bool hasWasmAppBundle)
    {
        string config = "Release";
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string csprojCode =
                $"""
                <Project Sdk="{sdk}">
                    <PropertyGroup>
                        <TargetFramework>{DefaultTargetFramework}</TargetFramework>
                        <RuntimeIdentifier>wasi-wasm</RuntimeIdentifier>
                        <WasmSingleFileBundle>true</WasmSingleFileBundle>
                        <OutputType>Library</OutputType>
                        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                    </PropertyGroup>
                </Project>
                """;
        string code = File.ReadAllText(Path.Combine(BuildEnvironment.TestAssetsPath, "LibraryMode.cs"));
        File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), code);
        File.WriteAllText(Path.Combine(_projectDir!, $"{id}.csproj"), csprojCode);
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        var buildArgs = new BuildArgs(projectName, config, AOT: false, ProjectFileContents: id, ExtraBuildArgs: null);
        buildArgs = ExpandBuildArgs(buildArgs);
        (_, string output) = BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: false,
                        CreateProject: false,
                        Publish: false,
                        AssertAppBundle: hasWasmAppBundle,
                        TargetFramework: BuildTestBase.DefaultTargetFramework
                        ));
    }
}
