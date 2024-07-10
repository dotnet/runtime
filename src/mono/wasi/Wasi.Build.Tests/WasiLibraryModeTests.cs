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

    [Fact]
    public void ConsoleBuildLibraryMode()
    {
        string config = "Release";
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string code =
                """
                using System;
                using System.Runtime.InteropServices;
                public unsafe class Test
                {
                    [UnmanagedCallersOnly(EntryPoint = "MyCallback")]
                    public static int MyCallback()
                    {
                        Console.WriteLine("WASM Library MyCallback is called");
                        return 100;
                    }
                }
                """;
        string csprojCode = 
                """
                <Project Sdk="Microsoft.NET.Sdk.WebAssembly">
                    <PropertyGroup>
                        <TargetFramework>net9.0</TargetFramework>
                        <RuntimeIdentifier>wasi-wasm</RuntimeIdentifier>
                        <WasmSingleFileBundle>true</WasmSingleFileBundle>
                        <OutputType>Library</OutputType>
                        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                    </PropertyGroup>
                </Project>
                """;
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
                        TargetFramework: BuildTestBase.DefaultTargetFramework
                        ));

        Assert.Contains("Build succeeded.", output);
    }
}
