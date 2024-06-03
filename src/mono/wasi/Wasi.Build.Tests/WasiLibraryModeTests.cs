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
        File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), code);
        string extraProperties = @"<WasmBuildNative>true</WasmBuildNative>
                                   <WasmNativeStrip>false</WasmNativeStrip>
                                   <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                                   <WasmSingleFileBundle>true</WasmSingleFileBundle>
                                   <OutputType>Library</OutputType>";
        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        var buildArgs = new BuildArgs(projectName, config, AOT: false, ProjectFileContents: id, ExtraBuildArgs: null);
        buildArgs = ExpandBuildArgs(buildArgs);
        (_, string output) = BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: false,
                        CreateProject: false,
                        Publish: true
                        ));

        Assert.Contains("Build succeeded.", output);
    }
}
