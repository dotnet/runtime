// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class PInvokeTableGeneratorTests : TestMainJsTestBase
    {
        public PInvokeTableGeneratorTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun(host: RunHost.Chrome)]
        public void NativeLibraryWithVariadicFunctions(BuildArgs buildArgs, RunHost host, string id)
        {
            string code = @"
                using System;
                using System.Runtime.InteropServices;
                public class Test
                {
                    public static int Main(string[] args)
                    {
                        Console.WriteLine($""Main running"");
                        if (args.Length > 2)
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

            (buildArgs, string output) = BuildForVariadicFunctionTests(code,
                                                          buildArgs with { ProjectName = $"variadic_{buildArgs.Config}_{id}" },
                                                          id);
            Assert.Matches("warning.*native function.*sum.*varargs", output);
            Assert.Contains("System.Int32 sum_one(System.Int32)", output);
            Assert.Contains("System.Int32 sum_two(System.Int32, System.Int32)", output);
            Assert.Contains("System.Int32 sum_three(System.Int32, System.Int32, System.Int32)", output);

            output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("Main running", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.Chrome)]
        public void DllImportWithFunctionPointersCompilesWithoutWarning(BuildArgs buildArgs, RunHost host, string id)
        {
            string code =
                """
                using System;
                using System.Runtime.InteropServices;
                public class Test
                {
                    public static int Main()
                    {
                        Console.WriteLine("Main running");
                        return 42;
                    }

                    [DllImport("variadic", EntryPoint="sum")]
                    public unsafe static extern int using_sum_one(delegate* unmanaged<char*, IntPtr, void> callback);

                    [DllImport("variadic", EntryPoint="sum")]
                    public static extern int sum_one(int a, int b);
                }
                """;

            (buildArgs, string output) = BuildForVariadicFunctionTests(code,
                                                          buildArgs with { ProjectName = $"fnptr_{buildArgs.Config}_{id}" },
                                                          id);

            Assert.DoesNotMatch("warning\\sWASM0001.*Could\\snot\\sget\\spinvoke.*Parsing\\sfunction\\spointer\\stypes", output);
            Assert.DoesNotMatch("warning\\sWASM0001.*Skipping.*using_sum_one.*because.*function\\spointer", output);

            output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("Main running", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.Chrome)]
        public void DllImportWithFunctionPointers_ForVariadicFunction_CompilesWithWarning(BuildArgs buildArgs, RunHost host, string id)
        {
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

                    [DllImport(""variadic"", EntryPoint=""sum"")]
                    public unsafe static extern int using_sum_one(delegate* unmanaged<char*, IntPtr, void> callback);
                }";

            (buildArgs, string output) = BuildForVariadicFunctionTests(code,
                                                          buildArgs with { ProjectName = $"fnptr_variadic_{buildArgs.Config}_{id}" },
                                                          id);

            Assert.DoesNotMatch("warning\\sWASM0001.*Could\\snot\\sget\\spinvoke.*Parsing\\sfunction\\spointer\\stypes", output);
            Assert.DoesNotMatch("warning\\sWASM0001.*Skipping.*using_sum_one.*because.*function\\spointer", output);

            output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("Main running", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.None)]
        public void UnmanagedStructAndMethodIn_SameAssembly_WithoutDisableRuntimeMarshallingAttribute_NotConsideredBlittable
                        (BuildArgs buildArgs, string id)
        {
            (_, string output) = SingleProjectForDisabledRuntimeMarshallingTest(
                withDisabledRuntimeMarshallingAttribute: false,
                withAutoLayout: true,
                expectSuccess: false,
                buildArgs,
                id
            );

            Assert.Matches("error.*Parameter.*types.*pinvoke.*.*blittable", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.None)]
        public void UnmanagedStructAndMethodIn_SameAssembly_WithoutDisableRuntimeMarshallingAttribute_WithStructLayout_ConsideredBlittable
                        (BuildArgs buildArgs, string id)
        {
            (_, string output) = SingleProjectForDisabledRuntimeMarshallingTest(
                withDisabledRuntimeMarshallingAttribute: false,
                withAutoLayout: false,
                expectSuccess: true,
                buildArgs,
                id
            );

            Assert.DoesNotMatch("error.*Parameter.*types.*pinvoke.*.*blittable", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.Chrome)]
        public void UnmanagedStructAndMethodIn_SameAssembly_WithDisableRuntimeMarshallingAttribute_ConsideredBlittable
                        (BuildArgs buildArgs, RunHost host, string id)
        {
            (buildArgs, _) = SingleProjectForDisabledRuntimeMarshallingTest(
                withDisabledRuntimeMarshallingAttribute: true,
                withAutoLayout: true,
                expectSuccess: true,
                buildArgs,
                id
            );

            string output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("Main running 5", output);
        }

        private (BuildArgs buildArgs ,string output) SingleProjectForDisabledRuntimeMarshallingTest(
            bool withDisabledRuntimeMarshallingAttribute, bool withAutoLayout,
            bool expectSuccess, BuildArgs buildArgs, string id
        ) {
            string code =
            """
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            """
            + (withDisabledRuntimeMarshallingAttribute ? "[assembly: DisableRuntimeMarshalling]" : "")
            + """
            public class Test
            {
                public static int Main()
                {
                    var x = new S { Value = 5 };

                    Console.WriteLine("Main running " + x.Value);
                    return 42;
                }
            """
            + (withAutoLayout ? "\n[StructLayout(LayoutKind.Auto)]\n" : "")
            + """
                public struct S { public int Value; public float Value2; }

                [UnmanagedCallersOnly]
                public static void M(S myStruct) { }
            }
            """;

            buildArgs = ExpandBuildArgs(
                buildArgs with { ProjectName = $"not_blittable_{buildArgs.Config}_{id}" },
                extraProperties: buildArgs.AOT
                    ? string.Empty
                    : "<WasmBuildNative>true</WasmBuildNative>"
            );

            (_, string output) = BuildProject(
                buildArgs,
                id: id,
                new BuildProjectOptions(
                    InitProject: () =>
                    {
                        File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), code);
                    },
                    Publish: buildArgs.AOT,
                    DotnetWasmFromRuntimePack: false,
                    ExpectSuccess: expectSuccess
                )
            );

            return (buildArgs, output);
        }

        public static IEnumerable<object?[]> SeparateAssemblyWithDisableMarshallingAttributeTestData(string config)
            => ConfigWithAOTData(aot: false, config: config).Multiply(
                    new object[] { /*libraryHasAttribute*/ false, /*appHasAttribute*/ false, /*expectSuccess*/ false },
                    new object[] { /*libraryHasAttribute*/ true, /*appHasAttribute*/ false, /*expectSuccess*/ false },
                    new object[] { /*libraryHasAttribute*/ false, /*appHasAttribute*/ true, /*expectSuccess*/ true },
                    new object[] { /*libraryHasAttribute*/ true, /*appHasAttribute*/ true, /*expectSuccess*/ true }
                ).WithRunHosts(RunHost.Chrome).UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(SeparateAssemblyWithDisableMarshallingAttributeTestData), parameters: "Debug")]
        [MemberData(nameof(SeparateAssemblyWithDisableMarshallingAttributeTestData), parameters: "Release")]
        public void UnmanagedStructsAreConsideredBlittableFromDifferentAssembly
                        (BuildArgs buildArgs, bool libraryHasAttribute, bool appHasAttribute, bool expectSuccess, RunHost host, string id)
            => SeparateAssembliesForDisableRuntimeMarshallingTest(
                libraryHasAttribute: libraryHasAttribute,
                appHasAttribute: appHasAttribute,
                expectSuccess: expectSuccess,
                buildArgs,
                host,
                id
            );

        private void SeparateAssembliesForDisableRuntimeMarshallingTest
                        (bool libraryHasAttribute, bool appHasAttribute, bool expectSuccess, BuildArgs buildArgs, RunHost host, string id)
        {
            string code =
                (libraryHasAttribute ? "[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]" : "")
                + "public struct __NonBlittableTypeForAutomatedTests__ { } public struct S { public int Value; public __NonBlittableTypeForAutomatedTests__ NonBlittable; }";

            var libraryBuildArgs = ExpandBuildArgs(
                buildArgs with { ProjectName = $"blittable_different_library_{buildArgs.Config}_{id}" },
                extraProperties: "<OutputType>Library</OutputType><RuntimeIdentifier />"
            );

            (string libraryDir, string output) = BuildProject(
                libraryBuildArgs,
                id: id + "_library",
                new BuildProjectOptions(
                    InitProject: () =>
                    {
                        File.WriteAllText(Path.Combine(_projectDir!, "S.cs"), code);
                    },
                    Publish: buildArgs.AOT,
                    DotnetWasmFromRuntimePack: false,
                    AssertAppBundle: false
                )
            );

            code =
            """
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            """
            + (appHasAttribute ? "[assembly: DisableRuntimeMarshalling]" : "")
            + """

            public class Test
            {
                public static int Main()
                {
                    var x = new S { Value = 5 };

                    Console.WriteLine("Main running " + x.Value);
                    return 42;
                }

                [UnmanagedCallersOnly]
                public static void M(S myStruct) { }
            }
            """;

            buildArgs = ExpandBuildArgs(
                buildArgs with { ProjectName = $"blittable_different_app_{buildArgs.Config}_{id}" },
                extraItems: $@"<ProjectReference Include='{Path.Combine(libraryDir, libraryBuildArgs.ProjectName + ".csproj")}' />",
                extraProperties: buildArgs.AOT
                    ? string.Empty
                    : "<WasmBuildNative>true</WasmBuildNative>"
            );

            _projectDir = null;

            (_, output) = BuildProject(
                buildArgs,
                id: id,
                new BuildProjectOptions(
                    InitProject: () =>
                    {
                        File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), code);
                    },
                    Publish: buildArgs.AOT,
                    DotnetWasmFromRuntimePack: false,
                    ExpectSuccess: expectSuccess
                )
            );

            if (expectSuccess)
            {
                output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
                Assert.Contains("Main running 5", output);
            }
            else
            {
                Assert.Matches("error.*Parameter.*types.*pinvoke.*.*blittable", output);
            }
        }

        [Theory]
        [BuildAndRun(host: RunHost.Chrome)]
        public void DllImportWithFunctionPointers_WarningsAsMessages(BuildArgs buildArgs, RunHost host, string id)
        {
            string code =
                """
                using System;
                using System.Runtime.InteropServices;
                public class Test
                {
                    public static int Main()
                    {
                        Console.WriteLine("Main running");
                        return 42;
                    }

                    [DllImport("someting")]
                    public unsafe static extern void SomeFunction1(delegate* unmanaged<int> callback);
                }
                """;

            (buildArgs, string output) = BuildForVariadicFunctionTests(
                code,
                buildArgs with { ProjectName = $"fnptr_{buildArgs.Config}_{id}" },
                id,
                verbosity: "normal",
                extraProperties: "<MSBuildWarningsAsMessages>$(MSBuildWarningsAsMessage);WASM0001</MSBuildWarningsAsMessages>"
            );

            Assert.DoesNotContain("warning WASM0001", output);

            output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("Main running", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.None)]
        public void UnmanagedCallback_WithFunctionPointers_CompilesWithWarnings(BuildArgs buildArgs, string id)
        {
            string code =
                """
                using System;
                using System.Runtime.InteropServices;
                public class Test
                {
                    public static int Main()
                    {
                        Console.WriteLine("Main running");
                        return 42;
                    }

                    [UnmanagedCallersOnly]
                    public unsafe static extern void SomeFunction1(delegate* unmanaged<int> callback);
                }
                """;

            (_, string output) = BuildForVariadicFunctionTests(
                code,
                buildArgs with { ProjectName = $"cb_fnptr_{buildArgs.Config}" },
                id
            );

            Assert.DoesNotMatch("warning\\sWASM0001.*Skipping.*Test::SomeFunction1.*because.*function\\spointer", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.Chrome)]
        public void UnmanagedCallback_InFileType(BuildArgs buildArgs, RunHost host, string id)
        {
            string code =
                """
                using System;
                using System.Runtime.InteropServices;
                public class Test
                {
                    public static int Main()
                    {
                        Console.WriteLine("Main running");
                        return 42;
                    }
                }

                file class Foo
                {
                    [UnmanagedCallersOnly]
                    public unsafe static extern void SomeFunction1(int i);
                }
                """;

            (buildArgs, string output) = BuildForVariadicFunctionTests(
                code,
                buildArgs with { ProjectName = $"cb_filetype_{buildArgs.Config}" },
                id
            );

            Assert.DoesNotMatch(".*(warning|error).*>[A-Z0-9]+__Foo", output);

            output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("Main running", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.None)]
        public void IcallWithOverloadedParametersAndEnum(BuildArgs buildArgs, string id)
        {
            // Build a library containing icalls with overloaded parameters.

            string code =
            """
            using System;
            using System.Runtime.CompilerServices;

            public static class Interop
            {
                public enum Numbers { A, B, C, D }

                [MethodImplAttribute(MethodImplOptions.InternalCall)]
                internal static extern void Square(Numbers x);

                [MethodImplAttribute(MethodImplOptions.InternalCall)]
                internal static extern void Square(Numbers x, Numbers y);

                public static void Main()
                {
                    // Noop
                }
            }
            """;

            var libraryBuildArgs = ExpandBuildArgs(
                buildArgs with { ProjectName = $"icall_enum_library_{buildArgs.Config}_{id}" }
            );

            (string libraryDir, string output) = BuildProject(
                libraryBuildArgs,
                id: id + "library",
                new BuildProjectOptions(
                    InitProject: () =>
                    {
                        File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), code);
                    },
                    Publish: false,
                    DotnetWasmFromRuntimePack: false,
                    AssertAppBundle: false
                )
            );

            // Build a project with ManagedToNativeGenerator task reading icalls from the above library and runtime-icall-table.h bellow.

            string projectCode =
            """
            <Project>
                <UsingTask TaskName="ManagedToNativeGenerator" AssemblyFile="###WasmAppBuilder###" />
                <Target Name="Build">
                  <PropertyGroup>
                    <WasmPInvokeTablePath>pinvoke-table.h</WasmPInvokeTablePath>
                    <WasmInterpToNativeTablePath>wasm_m2n_invoke.g.h</WasmInterpToNativeTablePath>
                    <WasmRuntimeICallTablePath>runtime-icall-table.h</WasmRuntimeICallTablePath>
                  </PropertyGroup>

                  <ItemGroup>
                    <WasmPInvokeModule Include="libSystem.Native" />
                    ###WasmPInvokeModule###
                  </ItemGroup>

                  <ManagedToNativeGenerator
                    Assemblies="@(WasmPInvokeAssembly)"
                    PInvokeModules="@(WasmPInvokeModule)"
                    PInvokeOutputPath="$(WasmPInvokeTablePath)"
                    RuntimeIcallTableFile="$(WasmRuntimeICallTablePath)"
                    InterpToNativeOutputPath="$(WasmInterpToNativeTablePath)">
                    <Output TaskParameter="FileWrites" ItemName="FileWrites" />
                  </ManagedToNativeGenerator>
                </Target>
            </Project>
            """;

            string AddAssembly(string name) => $"<WasmPInvokeAssembly Include=\"{Path.Combine(libraryDir, "bin", buildArgs.Config, DefaultTargetFramework, "browser-wasm", name + ".dll")}\" />";

            string icallTable =
            """
            [
             { "klass":"Interop", "icalls": [{} 	,{ "name": "Square(Numbers)", "func": "ves_abc", "handles": false }
            	,{ "name": "Add(Numbers,Numbers)", "func": "ves_def", "handles": false }
            ]}
            ]

            """;

            string tasksDir = Path.Combine(s_buildEnv.WorkloadPacksDir,
                                                              "Microsoft.NET.Runtime.WebAssembly.Sdk",
                                                              s_buildEnv.GetRuntimePackVersion(DefaultTargetFramework),
                                                              "tasks",
                                                              BuildTestBase.TargetFrameworkForTasks); // not net472!
            if (!Directory.Exists(tasksDir)) {
                string? tasksDirParent = Path.GetDirectoryName (tasksDir);
                if (!string.IsNullOrEmpty (tasksDirParent)) {
                    if (!Directory.Exists(tasksDirParent)) {
                        _testOutput.WriteLine($"Expected {tasksDirParent} to exist and contain TFM subdirectories");
                    }
                    _testOutput.WriteLine($"runtime pack tasks dir {tasksDir} contains subdirectories:");
                    foreach (string subdir in Directory.EnumerateDirectories(tasksDirParent)) {
                        _testOutput.WriteLine($"  - {subdir}");
                    }
                }
                throw new DirectoryNotFoundException($"Could not find tasks directory {tasksDir}");
            }

            string? taskPath = Directory.EnumerateFiles(tasksDir, "WasmAppBuilder.dll", SearchOption.AllDirectories)
                                            .FirstOrDefault();
            if (string.IsNullOrEmpty(taskPath))
                throw new FileNotFoundException($"Could not find WasmAppBuilder.dll in {tasksDir}");

            _testOutput.WriteLine ("Using WasmAppBuilder.dll from {0}", taskPath);

            projectCode = projectCode
                .Replace("###WasmPInvokeModule###", AddAssembly("System.Private.CoreLib") + AddAssembly("System.Runtime") + AddAssembly(libraryBuildArgs.ProjectName))
                .Replace("###WasmAppBuilder###", taskPath);

            buildArgs = buildArgs with { ProjectName = $"icall_enum_{buildArgs.Config}_{id}", ProjectFileContents = projectCode };

            _projectDir = null;

            (_, output) = BuildProject(
                buildArgs,
                id: id + "tasks",
                new BuildProjectOptions(
                    InitProject: () =>
                    {
                        File.WriteAllText(Path.Combine(_projectDir!, "runtime-icall-table.h"), icallTable);
                    },
                    Publish: buildArgs.AOT,
                    DotnetWasmFromRuntimePack: false,
                    UseCache: false,
                    AssertAppBundle: false
                )
            );

            Assert.DoesNotMatch(".*warning.*Numbers", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.Chrome, parameters: new object[] { "tr_TR.UTF-8" })]
        public void BuildNativeInNonEnglishCulture(BuildArgs buildArgs, string culture, RunHost host, string id)
        {
            // Check that we can generate interp tables in non-english cultures
            // Prompted by https://github.com/dotnet/runtime/issues/71149

            string code = @"
                using System;
                using System.Runtime.InteropServices;

                Console.WriteLine($""square: {square(5)}"");
                return 42;

                [DllImport(""simple"")] static extern int square(int x);
            ";

            buildArgs = ExpandBuildArgs(buildArgs,
                                        extraItems: @$"<NativeFileReference Include=""simple.c"" />",
                                        extraProperties: buildArgs.AOT
                                                            ? string.Empty
                                                            : "<WasmBuildNative>true</WasmBuildNative>");

            var extraEnvVars = new Dictionary<string, string> {
                { "LANG", culture },
                { "LC_ALL", culture },
            };

            (_, string output) = BuildProject(buildArgs,
                                        id: id,
                                        new BuildProjectOptions(
                                            InitProject: () =>
                                            {
                                                File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), code);
                                                File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "simple.c"),
                                                            Path.Combine(_projectDir!, "simple.c"));
                                            },
                                            Publish: buildArgs.AOT,
                                            DotnetWasmFromRuntimePack: false,
                                            ExtraBuildEnvironmentVariables: extraEnvVars
                                            ));

            output = RunAndTestWasmApp(buildArgs,
                                       buildDir: _projectDir,
                                       expectedExitCode: 42,
                                       host: host,
                                       id: id,
                                       envVars: extraEnvVars);
            Assert.Contains("square: 25", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.Chrome, parameters: new object[] { new object[] {
                "with-hyphen",
                "with#hash-and-hyphen",
                "with.per.iod",
                "with🚀unicode#"
            } })]

        public void CallIntoLibrariesWithNonAlphanumericCharactersInTheirNames(BuildArgs buildArgs, string[] libraryNames, RunHost host, string id)
        {
            buildArgs = ExpandBuildArgs(buildArgs,
                                        extraItems: @$"<NativeFileReference Include=""*.c"" />",
                                        extraProperties: buildArgs.AOT
                                                            ? string.Empty
                                                            : "<WasmBuildNative>true</WasmBuildNative>");

            int baseArg = 10;
            (_, string output) = BuildProject(buildArgs,
                                        id: id,
                                        new BuildProjectOptions(
                                            InitProject: () => GenerateSourceFiles(_projectDir!, baseArg),
                                            Publish: buildArgs.AOT,
                                            DotnetWasmFromRuntimePack: false
                                            ));

            output = RunAndTestWasmApp(buildArgs,
                                       buildDir: _projectDir,
                                       expectedExitCode: 42,
                                       host: host,
                                       id: id);

            for (int i = 0; i < libraryNames.Length; i ++)
            {
                Assert.Contains($"square_{i}: {(i + baseArg) * (i + baseArg)}", output);
            }

            void GenerateSourceFiles(string outputPath, int baseArg)
            {
                StringBuilder csBuilder = new($@"
                    using System;
                    using System.Runtime.InteropServices;
                ");

                StringBuilder dllImportsBuilder = new();
                for (int i = 0; i < libraryNames.Length; i ++)
                {
                    dllImportsBuilder.AppendLine($"[DllImport(\"{libraryNames[i]}\")] static extern int square_{i}(int x);");
                    csBuilder.AppendLine($@"Console.WriteLine($""square_{i}: {{square_{i}({i + baseArg})}}"");");

                    string nativeCode = $@"
                        #include <stdarg.h>

                        int square_{i}(int x)
                        {{
                            return x * x;
                        }}";
                    File.WriteAllText(Path.Combine(outputPath, $"{libraryNames[i]}.c"), nativeCode);
                }

                csBuilder.AppendLine("return 42;");
                csBuilder.Append(dllImportsBuilder);

                File.WriteAllText(Path.Combine(outputPath, "Program.cs"), csBuilder.ToString());
            }
        }

        private (BuildArgs, string) BuildForVariadicFunctionTests(string programText, BuildArgs buildArgs, string id, string? verbosity = null, string extraProperties = "")
        {
            extraProperties += "<AllowUnsafeBlocks>true</AllowUnsafeBlocks><_WasmDevel>true</_WasmDevel>";

            string filename = "variadic.o";
            buildArgs = ExpandBuildArgs(buildArgs,
                                        extraItems: $"<NativeFileReference Include=\"{filename}\" />",
                                        extraProperties: extraProperties);

            (_, string output) = BuildProject(buildArgs,
                                        id: id,
                                        new BuildProjectOptions(
                                            InitProject: () =>
                                            {
                                                File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText);
                                                File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", filename),
                                                            Path.Combine(_projectDir!, filename));
                                            },
                                            Publish: buildArgs.AOT,
                                            Verbosity: verbosity,
                                            DotnetWasmFromRuntimePack: false));

            return (buildArgs, output);
        }

        private void EnsureWasmAbiRulesAreFollowed(BuildArgs buildArgs, RunHost host, string id)
        {
            string programText = @"
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                public struct SingleFloatStruct {
                    public float Value;
                }
                public struct SingleDoubleStruct {
                    public struct Nested1 {
                        // This field is private on purpose to ensure we treat visibility correctly
                        double Value;
                    }
                    public Nested1 Value;
                }
                public struct SingleI64Struct {
                    public Int64 Value;
                }

                public class Test
                {
                    public static unsafe int Main(string[] argv)
                    {
                        var i64_a = 0xFF00FF00FF00FF0L;
                        var i64_b = ~i64_a;
                        var resI = direct64(i64_a);
                        Console.WriteLine(""l (l)="" + resI);

                        var sis = new SingleI64Struct { Value = i64_a };
                        var resSI = indirect64(sis);
                        Console.WriteLine(""s (s)="" + resSI.Value);

                        var resF = direct(3.14);
                        Console.WriteLine(""f (d)="" + resF);

                        SingleDoubleStruct sds = default;
                        Unsafe.As<SingleDoubleStruct, double>(ref sds) = 3.14;

                        resF = indirect_arg(sds);
                        Console.WriteLine(""f (s)="" + resF);

                        var res = indirect(sds);
                        Console.WriteLine(""s (s)="" + res.Value);

                        return (int)res.Value;
                    }

                    [DllImport(""wasm-abi"", EntryPoint=""accept_double_struct_and_return_float_struct"")]
                    public static extern SingleFloatStruct indirect(SingleDoubleStruct arg);

                    [DllImport(""wasm-abi"", EntryPoint=""accept_double_struct_and_return_float_struct"")]
                    public static extern float indirect_arg(SingleDoubleStruct arg);

                    [DllImport(""wasm-abi"", EntryPoint=""accept_double_struct_and_return_float_struct"")]
                    public static extern float direct(double arg);

                    [DllImport(""wasm-abi"", EntryPoint=""accept_and_return_i64_struct"")]
                    public static extern SingleI64Struct indirect64(SingleI64Struct arg);

                    [DllImport(""wasm-abi"", EntryPoint=""accept_and_return_i64_struct"")]
                    public static extern Int64 direct64(Int64 arg);
                }";

            var extraProperties = "<AllowUnsafeBlocks>true</AllowUnsafeBlocks><_WasmDevel>true</_WasmDevel>";
            var extraItems = @"<NativeFileReference Include=""wasm-abi.c"" />";

            buildArgs = ExpandBuildArgs(buildArgs,
                                        extraItems: extraItems,
                                        extraProperties: extraProperties);

            (string libraryDir, string output) = BuildProject(buildArgs,
                                        id: id,
                                        new BuildProjectOptions(
                                            InitProject: () =>
                                            {
                                                File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText);
                                                File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "wasm-abi.c"),
                                                            Path.Combine(_projectDir!, "wasm-abi.c"));
                                            },
                                            Publish: buildArgs.AOT,
                                            // Verbosity: "diagnostic",
                                            DotnetWasmFromRuntimePack: false));

            string objDir = Path.Combine(_projectDir!, "obj", buildArgs.Config!, "net9.0", "browser-wasm", "wasm", buildArgs.AOT ? "for-publish" : "for-build");

            // Verify that the right signature was added for the pinvoke. We can't determine this by examining the m2n file
            // FIXME: Not possible in in-process mode for some reason, even with verbosity at "diagnostic"
            // Assert.Contains("Adding pinvoke signature FD for method 'Test.", output);

            string pinvokeTable = File.ReadAllText(Path.Combine(objDir, "pinvoke-table.h"));
            // Verify that the invoke is in the pinvoke table. Under various circumstances we will silently skip it,
            //  for example if the module isn't found
            Assert.Contains("\"accept_double_struct_and_return_float_struct\", accept_double_struct_and_return_float_struct", pinvokeTable);
            // Verify the signature of the C function prototype. Wasm ABI specifies that the structs should both decompose into scalars.
            Assert.Contains("float accept_double_struct_and_return_float_struct (double);", pinvokeTable);
            Assert.Contains("int64_t accept_and_return_i64_struct (int64_t);", pinvokeTable);

            var runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 3, host: host, id: id);
            Assert.Contains("l (l)=-1148435428713435121", runOutput);
            Assert.Contains("s (s)=-1148435428713435121", runOutput);
            Assert.Contains("f (d)=3.14", runOutput);
            Assert.Contains("f (s)=3.14", runOutput);
            Assert.Contains("s (s)=3.14", runOutput);
        }

        [Theory]
        [BuildAndRun(host: RunHost.Chrome, aot: true)]
        public void EnsureWasmAbiRulesAreFollowedInAOT(BuildArgs buildArgs, RunHost host, string id) =>
            EnsureWasmAbiRulesAreFollowed(buildArgs, host, id);

        [Theory]
        [BuildAndRun(host: RunHost.Chrome, aot: false)]
        public void EnsureWasmAbiRulesAreFollowedInInterpreter(BuildArgs buildArgs, RunHost host, string id) =>
            EnsureWasmAbiRulesAreFollowed(buildArgs, host, id);
    }
}
