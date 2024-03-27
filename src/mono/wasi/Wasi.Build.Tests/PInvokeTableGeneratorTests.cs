// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;
using Wasm.Build.Tests;

#nullable enable

namespace Wasi.Build.Tests;

public class PInvokeTableGeneratorTests : BuildTestBase
{
    public PInvokeTableGeneratorTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public void InteropSupportForUnmanagedEntryPointWithoutDelegate()
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
                    [UnmanagedCallersOnly(EntryPoint = "ManagedFunc")]
                    public static int MyExport(int number)
                    {
                        // called from MyImport aka UnmanagedFunc
                        Console.WriteLine($"MyExport({number}) -> 42");
                        return 42;
                    }

                    [DllImport("*", EntryPoint = "UnmanagedFunc")]
                    public static extern void MyImport(); // calls ManagedFunc aka MyExport

                    public unsafe static int Main(string[] args)
                    {
                        Console.WriteLine($"main: {args.Length}");
                        MyImport();
                        return 0;
                    }
                }
                """;

        string projectName = Path.GetFileNameWithoutExtension(projectFile);

        var buildArgs = new BuildArgs(projectName, config, AOT: true, ProjectFileContents: id, ExtraBuildArgs: null);
        buildArgs = ExpandBuildArgs(buildArgs);
        AddItemsPropertiesToProject("<NativeFileReference Include=\"local.c\" />");
        AddItemsPropertiesToProject(projectFile, "<WasmSingleFileBundle>true</WasmSingleFileBundle>");

        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        InitProject: () =>
                        {
                            File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), code);
                        },
                        DotnetWasmFromRuntimePack: false,
                        Publish: true,
                        TargetFramework: BuildTestBase.DefaultTargetFramework));

        CommandResult res = new RunCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!)
                                    .ExecuteWithCapturedOutput($"run --no-silent --no-build -c {config}")
                                    .EnsureSuccessful();
        Assert.Contains("MyExport(123) -> 42", res.Output);
    }
}
