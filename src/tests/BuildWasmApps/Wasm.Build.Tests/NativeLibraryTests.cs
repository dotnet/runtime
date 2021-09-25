// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class NativeLibraryTests : BuildTestBase
    {
        public NativeLibraryTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [BuildAndRun(aot: false)]
        [BuildAndRun(aot: true)]
        public void ProjectWithNativeReference(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"AppUsingNativeLib-a";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraItems: "<NativeFileReference Include=\"native-lib.o\" />");

            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? _))
            {
                InitPaths(id);
                if (Directory.Exists(_projectDir))
                    Directory.Delete(_projectDir, recursive: true);

                Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "AppUsingNativeLib"), _projectDir);
                File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "native-lib.o"), Path.Combine(_projectDir, "native-lib.o"));
            }

            BuildProject(buildArgs,
                        dotnetWasmFromRuntimePack: false,
                        id: id);

            string output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 0,
                                test: output => {},
                                host: host, id: id);

            Assert.Contains("print_line: 100", output);
            Assert.Contains("from pinvoke: 142", output);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [BuildAndRun(aot: false)]
        [BuildAndRun(aot: true)]
        public void ProjectUsingSkiaSharp(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"AppUsingSkiaSharp";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                            extraItems: @$"
                                <PackageReference Include=""SkiaSharp"" Version=""2.80.3"" />
                                <PackageReference Include=""SkiaSharp.NativeAssets.WebAssembly"" Version=""2.80.3"" />

                                <NativeFileReference Include=""$(SkiaSharpStaticLibraryPath)\2.0.9\*.a"" />
                                <WasmFilesToIncludeInFileSystem Include=""{Path.Combine(BuildEnvironment.TestAssetsPath, "mono.png")}"" />
                            ");

            string programText = @"
using System;
using SkiaSharp;

public class Test
{
    public static int Main()
    {
        using SKFileStream skfs = new SKFileStream(""mono.png"");
        using SKImage img = SKImage.FromEncodedData(skfs);

        Console.WriteLine ($""Size: {skfs.Length} Height: {img.Height}, Width: {img.Width}"");
        return 0;
    }
}";

            BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                        dotnetWasmFromRuntimePack: false,
                        id: id);

            string output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 0,
                                test: output => {},
                                host: host, id: id,
                                args: "mono.png");

            Assert.Contains("Size: 26462 Height: 599, Width: 499", output);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [BuildAndRun(host: RunHost.V8, aot:false)]
        public void NativeLibraryWithVariadicFunctions(BuildArgs buildArgs, RunHost host, string id)
        {
            id = $"variadic_{buildArgs.Config}";
            string projectName = $"variadic_{buildArgs.Config}_{id}";
            string code = @"
using System;
using System.Runtime.InteropServices;
public class Test
{
    public static int Main(string[] args)
    {
        Console.WriteLine($""Main running"");
        if (args.Length > 0)
        {
            // We don't want to run this, because we can't call variadic functions
            Console.WriteLine($""sum_three: {sum_three(7, 14, 21)}"");
            Console.WriteLine($""sum_two: {sum_two(3, 6)}"");
            Console.WriteLine($""sum_one: {sum_one(5)}"");
        }
        return 42;
    }

    [DllImport(""variadic"", EntryPoint=""sum"")] public static extern int sum_one(int a);
    [DllImport(""variadic"", EntryPoint=""sum"")] public static extern int sum_two(int a, int b);
    [DllImport(""variadic"", EntryPoint=""sum"")] public static extern int sum_three(int a, int b, int c);
}";
            string filename = "variadic.o";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                                        extraItems: $"<NativeFileReference Include=\"{filename}\" />",
                                        extraProperties: "<_WasmDevel>true</_WasmDevel>");

            BuildProject(buildArgs,
                        initProject: () =>
                        {
                            File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), code);
                            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", filename),
                                        Path.Combine(_projectDir!, filename));
                        },
                        publish: false,
                        id: id,
                        dotnetWasmFromRuntimePack: false);

            string output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("Main running", output);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [BuildAndRun(host: RunHost.V8, aot:false)]
        public void DllImportWithFunctionPointersCompilesWithWarning(BuildArgs buildArgs, RunHost host, string id)
        {
            id= $"fnptr_{buildArgs.Config}";
            string projectName = id;
            string code = @"
using System;
using System.Runtime.InteropServices;
public class Test
{
    public static int Main()
    {
        Console.WriteLine($""Main running"");
        return 42;
    }

    [DllImport(""variadic"", EntryPoint=""sum"")] public unsafe static extern int using_sum_one(delegate* unmanaged<char*, IntPtr, void> callback);
    [DllImport(""variadic"", EntryPoint=""sum"")] public static extern int sum_one(int a, int b);
}";
            string filename = "variadic.o";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                                        extraItems: $"<NativeFileReference Include=\"{filename}\" />",
                                        extraProperties: "<AllowUnsafeBlocks>true</AllowUnsafeBlocks><_WasmDevel>true</_WasmDevel>");

            (_, string output) = BuildProject(buildArgs,
                                        initProject: () =>
                                        {
                                            File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), code);
                                            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", filename),
                                                        Path.Combine(_projectDir!, filename));
                                        },
                                        publish: false,
                                        id: id,
                                        dotnetWasmFromRuntimePack: false);

            Assert.Matches("warning.*Skipping.*because.*function pointer", output);
            Assert.Matches("warning.*using_sum_one", output);

            output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("Main running", output);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [BuildAndRun(host: RunHost.V8, aot:false)]
        public void DllImportWithFunctionPointers_ForVariadicFunction_CompilesWithWarning(BuildArgs buildArgs, RunHost host, string id)
        {
            id= $"fnptr_variadic_{buildArgs.Config}";
            string projectName = id;
            string code = @"
using System;
using System.Runtime.InteropServices;
public class Test
{
    public static int Main()
    {
        Console.WriteLine($""Main running"");
        return 42;
    }

    [DllImport(""variadic"", EntryPoint=""sum"")] public unsafe static extern int using_sum_one(delegate* unmanaged<char*, IntPtr, void> callback);
}";
            string filename = "variadic.o";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                                        extraItems: $"<NativeFileReference Include=\"{filename}\" />",
                                        extraProperties: "<AllowUnsafeBlocks>true</AllowUnsafeBlocks><_WasmDevel>true</_WasmDevel>");

            (_, string output) = BuildProject(buildArgs,
                                        initProject: () =>
                                        {
                                            File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), code);
                                            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", filename),
                                                        Path.Combine(_projectDir!, filename));
                                        },
                                        publish: false,
                                        id: id,
                                        dotnetWasmFromRuntimePack: false);

            Assert.Matches("warning.*Skipping.*because.*function pointer", output);
            Assert.Matches("warning.*using_sum_one", output);

            output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("Main running", output);
        }
    }
}
