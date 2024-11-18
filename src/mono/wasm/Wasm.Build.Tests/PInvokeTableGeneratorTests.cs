// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class PInvokeTableGeneratorTests : WasmTemplateTestsBase
    {
        public PInvokeTableGeneratorTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun(aot: false)]
        public async void NativeLibraryWithVariadicFunctions(Configuration config, bool aot)
        {
            ProjectInfo info = PrepreProjectForVariadicFuntion(config, aot, "variadic");
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "VariadicFunctions.cs"));
            string output = PublishForVariadicFunctionTests(info, config, aot);
            Assert.Matches("warning.*native function.*sum.*varargs", output);
            Assert.Contains("System.Int32 sum_one(System.Int32)", output);
            Assert.Contains("System.Int32 sum_two(System.Int32, System.Int32)", output);
            Assert.Contains("System.Int32 sum_three(System.Int32, System.Int32, System.Int32)", output);

            RunResult result = await RunForPublishWithWebServer(new(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42
            ));
            Assert.Contains("Main running", result.TestOutput);
        }

        [Theory]
        [BuildAndRun()]
        public async void DllImportWithFunctionPointersCompilesWithoutWarning(Configuration config, bool aot)
        {
            ProjectInfo info = PrepreProjectForVariadicFuntion(config, aot, "fnptr");
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "DllImportNoWarning.cs"));
            string output = PublishForVariadicFunctionTests(info, config, aot);

            Assert.DoesNotMatch("warning\\sWASM0001.*Could\\snot\\sget\\spinvoke.*Parsing\\sfunction\\spointer\\stypes", output);
            Assert.DoesNotMatch("warning\\sWASM0001.*Skipping.*using_sum_one.*because.*function\\spointer", output);

            RunResult result = await RunForPublishWithWebServer(new(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42
            ));
            Assert.Contains("Main running", result.TestOutput);
        }

        [Theory]
        [BuildAndRun()]
        public async void DllImportWithFunctionPointers_ForVariadicFunction_CompilesWithWarning(Configuration config, bool aot)
        {
            ProjectInfo info = PrepreProjectForVariadicFuntion(config, aot, "fnptr_variadic");
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "DllImportWarning.cs"));
            string output = PublishForVariadicFunctionTests(info, config, aot);

            Assert.DoesNotMatch("warning\\sWASM0001.*Could\\snot\\sget\\spinvoke.*Parsing\\sfunction\\spointer\\stypes", output);
            Assert.DoesNotMatch("warning\\sWASM0001.*Skipping.*using_sum_one.*because.*function\\spointer", output);

            RunResult result = await RunForPublishWithWebServer(new(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42
            ));
            Assert.Contains("Main running", result.TestOutput);
        }

        [Theory]
        [BuildAndRun()]
        public void UnmanagedStructAndMethodIn_SameAssembly_WithoutDisableRuntimeMarshallingAttribute_NotConsideredBlittable
                        (Configuration config, bool aot)
        {
            ProjectInfo info = PrepreProjectForBlittableTests(
                config, aot, "not_blittable", disableRuntimeMarshalling: false, useAutoLayout: true);
            (_, string output) = BuildProject(info, config, new PublishOptions(ExpectSuccess: false, AOT: aot));
            Assert.Matches("error.*Parameter.*types.*pinvoke.*.*blittable", output);
        }

        [Theory]
        [BuildAndRun()]
        public void UnmanagedStructAndMethodIn_SameAssembly_WithoutDisableRuntimeMarshallingAttribute_WithStructLayout_ConsideredBlittable
                        (Configuration config, bool aot)
        {
            ProjectInfo info = PrepreProjectForBlittableTests(
                config, aot, "blittable", disableRuntimeMarshalling: false, useAutoLayout: false);
            (_, string output) = BuildProject(info, config, isNativeBuild: true, AOT: aot);
            Assert.DoesNotMatch("error.*Parameter.*types.*pinvoke.*.*blittable", output);
        }

        [Theory]
        [BuildAndRun()]
        public async void UnmanagedStructAndMethodIn_SameAssembly_WithDisableRuntimeMarshallingAttribute_ConsideredBlittable
                        (Configuration config, bool aot)
        {
            ProjectInfo info = PrepreProjectForBlittableTests(
                config, aot, "blittable", disableRuntimeMarshalling: true, useAutoLayout: true);
            (_, string output) = BuildProject(info, config, isNativeBuild: true, AOT: aot);
            RunResult result = await RunForBuildWithDotnetRun(new(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42
            ));
            Assert.Contains(result.TestOutput, m => m.Contains("Main running"));
        }

        private ProjectInfo PrepreProjectForBlittableTests(Configuration config, bool aot, string prefix, bool disableRuntimeMarshalling, bool useAutoLayout = false)
        {
            string extraProperties = aot ? string.Empty : "<WasmBuildNative>true</WasmBuildNative>";
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, prefix, extraProperties: extraProperties);
            string programRelativePath = Path.Combine("Common", "Program.cs");
            ReplaceFile(programRelativePath, Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "BittableSameAssembly.cs"));
            
            var replacements = new Dictionary<string, string> { };
            if (!disableRuntimeMarshalling)
            {
                replacements.Add("[assembly: DisableRuntimeMarshalling]", "");
            }
            if (!useAutoLayout)
            {
                replacements.Add("[StructLayout(LayoutKind.Auto)]", "");
            }
            if (replacements.Count > 0)
            {
                UpdateFile(programRelativePath, replacements);
            }
            return info;
        }

        public static IEnumerable<object?[]> SeparateAssemblyWithDisableMarshallingAttributeTestData(Configuration config)
            => ConfigWithAOTData(aot: false, config: config).Multiply(
                    new object[] { /*libraryHasAttribute*/ false, /*appHasAttribute*/ false, /*expectSuccess*/ false },
                    new object[] { /*libraryHasAttribute*/ true, /*appHasAttribute*/ false, /*expectSuccess*/ false },
                    new object[] { /*libraryHasAttribute*/ false, /*appHasAttribute*/ true, /*expectSuccess*/ true },
                    new object[] { /*libraryHasAttribute*/ true, /*appHasAttribute*/ true, /*expectSuccess*/ true }
                ).UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(SeparateAssemblyWithDisableMarshallingAttributeTestData), parameters: Configuration.Debug)]
        [MemberData(nameof(SeparateAssemblyWithDisableMarshallingAttributeTestData), parameters: Configuration.Release)]
        public async void UnmanagedStructsAreConsideredBlittableFromDifferentAssembly
                        (Configuration config, bool aot, bool libraryHasAttribute, bool appHasAttribute, bool expectSuccess)
        {
            string extraProperties = aot ? string.Empty : "<WasmBuildNative>true</WasmBuildNative>";
            string extraItems =  @$"<ProjectReference Include=""..\\Library\\Library.csproj"" />";
            string libRelativePath = Path.Combine("..", "Library", "Library.cs");
            string programRelativePath = Path.Combine("Common", "Program.cs");
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "blittable_different_library", extraProperties: extraProperties, extraItems: extraItems);
            ReplaceFile(libRelativePath, Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "BittableDifferentAssembly_Lib.cs"));
            ReplaceFile(programRelativePath, Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "BittableDifferentAssembly.cs"));
            if (!libraryHasAttribute)
            {
                UpdateFile(libRelativePath, new Dictionary<string, string> { { "[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]", "" } });
            }
            if (!appHasAttribute)
            {
                UpdateFile(programRelativePath, new Dictionary<string, string> { { "[assembly: DisableRuntimeMarshalling]", "" } });
            }
            (_, string output) = BuildProject(info,
                config,
                new BuildOptions(expectSuccess: expectSuccess, AOT: aot),
                isNativeBuild: true);
            if (expectSuccess)
            {
                RunResult result = await RunForBuildWithDotnetRun(new(
                    config,
                    TestScenario: "DotnetRun",
                    ExpectedExitCode: 42
                ));
                Assert.Contains("Main running 5", result.TestOutput);
            }
            else
            {
                Assert.Matches("error.*Parameter.*types.*pinvoke.*.*blittable", output);
            }
        }


        [Theory]
        [BuildAndRun()]
        public async void DllImportWithFunctionPointers_WarningsAsMessages(Configuration config, bool aot)
        {
            string extraProperties = "<MSBuildWarningsAsMessages>$(MSBuildWarningsAsMessage);WASM0001</MSBuildWarningsAsMessages>";
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "fnptr", extraProperties: extraProperties);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "FunctionPointers.cs"));

            string output = PublishForVariadicFunctionTests(info, config, aot, isNativeBuild: false);
            Assert.DoesNotContain("warning WASM0001", output);

            RunResult result = await RunForPublishWithWebServer(new(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42
            ));
            Assert.Contains("Main running", result.TestOutput);
        }

        [Theory]
        [BuildAndRun()]
        public void UnmanagedCallback_WithFunctionPointers_CompilesWithWarnings(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "cb_fnptr");
            string programRelativePath = Path.Combine("Common", "Program.cs");
            ReplaceFile(programRelativePath, Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "FunctionPointers.cs"));
            UpdateFile(programRelativePath, new Dictionary<string, string> { { "[DllImport(\"someting\")]", "[UnmanagedCallersOnly]" } });
            string output = PublishForVariadicFunctionTests(info, config, aot, isNativeBuild: false);
            Assert.DoesNotMatch("warning\\sWASM0001.*Skipping.*Test::SomeFunction1.*because.*function\\spointer", output);
        }

        [Theory]
        [BuildAndRun()]
        public async void UnmanagedCallback_InFileType(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "cb_filetype");
            string programRelativePath = Path.Combine("Common", "Program.cs");
            ReplaceFile(programRelativePath, Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "UnmanagedCallbackInFile.cs"));

            string output = PublishForVariadicFunctionTests(info, config, aot, isNativeBuild: false);
            Assert.DoesNotMatch(".*(warning|error).*>[A-Z0-9]+__Foo", output);

            RunResult result = await RunForPublishWithWebServer(new(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42
            ));
            Assert.Contains("Main running", result.TestOutput);
        }

        [Theory]
        [BuildAndRun()]
        [ActiveIssue("RuntimeError: null function or function signature mismatch")]
        public async void UnmanagedCallersOnly_Namespaced(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "cb_namespace");
            string programRelativePath = Path.Combine("Common", "Program.cs");
            ReplaceFile(programRelativePath, Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "UnmanagedCallbackNamespaced.cs"));

            string output = PublishForVariadicFunctionTests(info, config, aot, isNativeBuild: false);
            Assert.DoesNotMatch(".*(warning|error).*>[A-Z0-9]+__Foo", output);

            RunResult result = await RunForPublishWithWebServer(new(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42
            ));
            Assert.Contains("A.Conflict.C", result.TestOutput);
            Assert.Contains("B.Conflict.C", result.TestOutput);
            if (OperatingSystem.IsWindows()) {
                // Windows console unicode support is not great
                Assert.Contains("A.Conflict.C_", result.TestOutput);
                Assert.Contains("B.Conflict.C_", result.TestOutput);
            } else {
                Assert.Contains("A.Conflict.C_\U0001F412", result.TestOutput);
                Assert.Contains("B.Conflict.C_\U0001F412", result.TestOutput);
            }
        }

        [Theory]
        [BuildAndRun()]
        public void IcallWithOverloadedParametersAndEnum(Configuration config, bool aot)
        {
            string appendToTheEnd =
            """
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
            """;

            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "icall_enum", insertAtEnd: appendToTheEnd);
            // build a library containing icalls with overloaded parameters.
            ReplaceFile(Path.Combine("..", "Library", "Library.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "ICall_Lib.cs"));
            // temporarily change the project directory to build the library
            _projectDir = Path.Combine(_projectDir!, "..", "Library");
            bool isPublish = false;
            // libraries do not have framework dirs
            string hypotheticalFrameworkDir = Path.Combine(GetBinFrameworkDir(config, isPublish));
            string libAssemblyPath = Path.Combine(hypotheticalFrameworkDir, "..", "..");
            BuildProject(info, config, new BuildOptions(AssertAppBundle: false, AOT: aot));
            // restore the project directory
            _projectDir = Path.Combine(_projectDir!, "..", "App");
            
            string icallTable =
            """
            [
             { "klass":"Interop", "icalls": [{} 	,{ "name": "Square(Numbers)", "func": "ves_abc", "handles": false }
            	,{ "name": "Add(Numbers,Numbers)", "func": "ves_def", "handles": false }
            ]}
            ]

            """;
            UpdateFile(Path.Combine(_projectDir!, "runtime-icall-table.h"), icallTable);

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
            
            string AddAssembly(string assemblyLocation, string name) => $"<WasmPInvokeAssembly Include=\"{Path.Combine(assemblyLocation, name + ".dll")}\" />";
            string frameworkDir = Path.Combine(GetBinFrameworkDir(config, isPublish));
            string appAssemblyPath = Path.Combine(frameworkDir, "..", "..");
            string pinvokeReplacement =
                AddAssembly(appAssemblyPath, "System.Private.CoreLib") +
                AddAssembly(appAssemblyPath, "System.Runtime") +
                AddAssembly(libAssemblyPath, "Library");
            UpdateFile("WasmBasicTestApp.csproj", new Dictionary<string, string> {
                { "###WasmPInvokeModule###", pinvokeReplacement },
                { "###WasmAppBuilder###", taskPath }
            });

            // Build a project with ManagedToNativeGenerator task reading icalls from the above library and runtime-icall-table.h
            (_, string output) = BuildProject(info, config, new BuildOptions(UseCache: false, AOT: aot));
            Assert.DoesNotMatch(".*warning.*Numbers", output);
        }

        [Theory]
        [BuildAndRun(parameters: new object[] { "tr_TR.UTF-8" })]
        public async void BuildNativeInNonEnglishCulture(Configuration config, bool aot, string culture)
        {
            // Check that we can generate interp tables in non-english cultures
            // Prompted by https://github.com/dotnet/runtime/issues/71149

            string extraItems = @$"<NativeFileReference Include=""simple.c"" />";
            string extraProperties = aot ? string.Empty : "<WasmBuildNative>true</WasmBuildNative>";
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "buildNativeNonEng", extraItems: extraItems);
            string programRelativePath = Path.Combine("Common", "Program.cs");
            ReplaceFile(programRelativePath, Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "BuildNative.cs"));
            string cCodeFilename = "simple.c";
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", cCodeFilename), Path.Combine(_projectDir!, cCodeFilename));

            var extraEnvVars = new Dictionary<string, string> {
                { "LANG", culture },
                { "LC_ALL", culture },
            };
            bool isPublish = true;
            (_, string output) = PublishProject(info,
                config,
                new PublishOptions(ExtraBuildEnvironmentVariables: extraEnvVars, AOT: aot),
                isNativeBuild: true);

            RunResult result = await RunForPublishWithWebServer(new(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42,
                Locale: culture
            ));
            Assert.Contains("square: 25", result.TestOutput);
        }

        [Theory]
        [BuildAndRun(parameters: new object[] { new object[] {
                "with-hyphen",
                "with#hash-and-hyphen",
                "with.per.iod",
                "with🚀unicode#"
            } })]
        public async void CallIntoLibrariesWithNonAlphanumericCharactersInTheirNames(Configuration config, bool aot, string[] libraryNames)
        {
            var extraItems = @"<NativeFileReference Include=""*.c"" />";
            string extraProperties = aot ? string.Empty : "<WasmBuildNative>true</WasmBuildNative>";
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "abi", extraItems: extraItems, extraProperties: extraProperties);
            
            int baseArg = 10;
            GenerateSourceFiles(_projectDir!, baseArg);
            bool isPublish = aot;
            (_, string output) = isPublish ?
                PublishProject(info, config, new BuildOptions(AOT: aot), isNativeBuild: true):
                BuildProject(info, config, new PublishOptions(AOT: aot), isNativeBuild: true);

            var runOptions = new RunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 42);
            RunResult result = isPublish ? await RunForPublishWithWebServer(runOptions) : await RunForBuildWithDotnetRun(runOptions);

            for (int i = 0; i < libraryNames.Length; i ++)
            {
                Assert.Contains($"square_{i}: {(i + baseArg) * (i + baseArg)}", result.TestOutput);
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
                    csBuilder.AppendLine($@"Console.WriteLine($""TestOutput -> square_{i}: {{square_{i}({i + baseArg})}}"");");

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

                UpdateFile(Path.Combine("Common", "Program.cs"), csBuilder.ToString());
            }
        }

        private ProjectInfo PrepreProjectForVariadicFuntion(Configuration config, bool aot, string prefix, string extraProperties = "")
        {
            string objectFilename = "variadic.o";
            extraProperties += "<AllowUnsafeBlocks>true</AllowUnsafeBlocks><_WasmDevel>true</_WasmDevel>";
            string extraItems = $"<NativeFileReference Include=\"{objectFilename}\" />";
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, prefix, extraItems: extraItems, extraProperties: extraProperties);
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", objectFilename), Path.Combine(_projectDir!, objectFilename));
            return info;
        }

        private string PublishForVariadicFunctionTests(ProjectInfo info, Configuration config, bool aot, string? verbosity = null, bool isNativeBuild = true)
        {
            string verbosityArg = verbosity == null ? string.Empty : $" -v:{verbosity}";
            var extraArgs = new string[] { verbosityArg };
            // NativeFileReference forces native build
            (_, string output) = PublishProject(info,
                config,
                new PublishOptions(ExtraMSBuildArgs: extraArgs, AOT: aot),
                isNativeBuild: isNativeBuild);
            return output;
        }

        private async Task EnsureWasmAbiRulesAreFollowed(Configuration config, bool aot)
        {
            var extraItems = @"<NativeFileReference Include=""wasm-abi.c"" />";
            var extraProperties = "<AllowUnsafeBlocks>true</AllowUnsafeBlocks><_WasmDevel>false</_WasmDevel><WasmNativeStrip>false</WasmNativeStrip>";
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "abi", extraItems: extraItems, extraProperties: extraProperties);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "AbiRules.cs"));
            string cCodeFilename = "wasm-abi.c";
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", cCodeFilename), Path.Combine(_projectDir!, cCodeFilename));

            bool isPublish = aot;
            var options = isPublish ? PublishOptions(AOT: aot) : new BuildOptions(AOT: aot);
            (string _, string _) = isPublish ? PublishProject(info, options, isNativeBuild: true) : BuildProject(info, options, isNativeBuild: true);

            string objDir = Path.Combine(_projectDir!, "obj", config, buildOptions.TargetFramework, "wasm", isPublish ? "for-publish" : "for-build");

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

            var runOptions = new RunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 3);
            RunResult result = isPublish ? await RunForPublishWithWebServer(runOptions) : await RunForBuildWithDotnetRun(runOptions);
            Assert.Contains("l (l)=-1148435428713435121", result.TestOutput);
            Assert.Contains("s (s)=-1148435428713435121", result.TestOutput);
            Assert.Contains("f (d)=3.14", result.TestOutput);
            Assert.Contains("f (s)=3.14", result.TestOutput);
            Assert.Contains("s (s)=3.14", result.TestOutput);
            Assert.Contains("paires.B=4", result.TestOutput);
            Assert.Contains(result.TestOutput, m => m.Contains("iares[0]=32"));
            Assert.Contains(result.TestOutput, m => m.Contains("iares[1]=2"));
            Assert.Contains("fares.elements[1]=2", result.TestOutput);
        }

        [Theory]
        [BuildAndRun(aot: true, config: Configuration.Release)]
        public async void EnsureWasmAbiRulesAreFollowedInAOT(Configuration config, bool aot) =>
            await EnsureWasmAbiRulesAreFollowed(config, aot);

        [Theory]
        [BuildAndRun(aot: false)]
        public async void EnsureWasmAbiRulesAreFollowedInInterpreter(Configuration config, bool aot) =>
            await EnsureWasmAbiRulesAreFollowed(config, aot);

        [Theory]
        [BuildAndRun(aot: true, config: Configuration.Release)]
        [ActiveIssue("WasmGenerateAppBundle = false, so _WasmGenerateAppBundle is not triggered. Is the change expected?")]
        public void EnsureComInteropCompilesInAOT(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "com");
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "ComInterop.cs"));
            bool isPublish = aot;
            var options = isPublish ? PublishOptions(AOT: aot) : new BuildOptions(AOT: aot);
            (string libraryDir, string output) = isPublish ?
                PublishProject(info, options, new PublishOptions(AOT: aot), isNativeBuild) :
                BuildProject(info, options, new BuildOptions(AOT: aot), isNativeBuild: true);
            Assert.Contains("Generated app bundle at " + libraryDir, output);
        }

        [Theory]
        [BuildAndRun(aot: false)]
        public async void UCOWithSpecialCharacters(Configuration config, bool aot)
        {
            var extraProperties = "<AllowUnsafeBlocks>true</AllowUnsafeBlocks>";
            var extraItems = @"<NativeFileReference Include=""local.c"" />";
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "uoc", extraItems: extraItems, extraProperties: extraProperties);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "UnmanagedCallback.cs"));
            string cCodeFilename = "local.c";
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", cCodeFilename), Path.Combine(_projectDir!, cCodeFilename));

            PublishProject(info, config, new PublishOptions(AOT: aot), isNativeBuild: true);
            RunResult result = await RunForPublishWithWebServer(new(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42
            ));
            Assert.DoesNotContain("Conflict.A.Managed8\u4F60Func(123) -> 123", result.TestOutput);
            Assert.Contains("ManagedFunc returned 42", result.TestOutput);
        }
    }
}
