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
    public class DllImportTests : PInvokeTableGeneratorTestsBase
    {
        public DllImportTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun(aot: false)]
        public async Task NativeLibraryWithVariadicFunctions(Configuration config, bool aot)
        {
            ProjectInfo info = PrepareProjectForVariadicFunction(config, aot, "variadic");
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "VariadicFunctions.cs"));
            string output = PublishForVariadicFunctionTests(info, config, aot);
            Assert.Matches("warning.*native function.*sum.*varargs", output);
            Assert.Contains("System.Int32 sum_one(System.Int32)", output);
            Assert.Contains("System.Int32 sum_two(System.Int32, System.Int32)", output);
            Assert.Contains("System.Int32 sum_three(System.Int32, System.Int32, System.Int32)", output);

            RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42
            ));
            Assert.Contains("Main running", result.TestOutput);
        }

        [Theory]
        [BuildAndRun()]
        public async Task DllImportWithFunctionPointersCompilesWithoutWarning(Configuration config, bool aot)
        {
            ProjectInfo info = PrepareProjectForVariadicFunction(config, aot, "fnptr");
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "DllImportNoWarning.cs"));
            string output = PublishForVariadicFunctionTests(info, config, aot);

            Assert.DoesNotMatch("warning\\sWASM0001.*Could\\snot\\sget\\spinvoke.*Parsing\\sfunction\\spointer\\stypes", output);
            Assert.DoesNotMatch("warning\\sWASM0001.*Skipping.*using_sum_one.*because.*function\\spointer", output);

            RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42
            ));
            Assert.Contains("Main running", result.TestOutput);
        }

        [Theory]
        [BuildAndRun()]
        public async Task DllImportWithFunctionPointers_ForVariadicFunction_CompilesWithWarning(Configuration config, bool aot)
        {
            ProjectInfo info = PrepareProjectForVariadicFunction(config, aot, "fnptr_variadic");
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "DllImportWarning.cs"));
            string output = PublishForVariadicFunctionTests(info, config, aot);

            Assert.DoesNotMatch("warning\\sWASM0001.*Could\\snot\\sget\\spinvoke.*Parsing\\sfunction\\spointer\\stypes", output);
            Assert.DoesNotMatch("warning\\sWASM0001.*Skipping.*using_sum_one.*because.*function\\spointer", output);

            RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42
            ));
            Assert.Contains("Main running", result.TestOutput);
        }

        [Theory]
        [BuildAndRun()]
        public async Task DllImportWithFunctionPointers_WarningsAsMessages(Configuration config, bool aot)
        {
            string extraProperties = "<MSBuildWarningsAsMessages>$(MSBuildWarningsAsMessage);WASM0001</MSBuildWarningsAsMessages>";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "fnptr", extraProperties: extraProperties);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "FunctionPointers.cs"));

            string output = PublishForVariadicFunctionTests(info, config, aot);
            Assert.DoesNotContain("warning WASM0001", output);

            RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(
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
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "cb_fnptr");
            string programRelativePath = Path.Combine("Common", "Program.cs");
            ReplaceFile(programRelativePath, Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "PInvoke", "FunctionPointers.cs"));
            UpdateFile(programRelativePath, new Dictionary<string, string> { { "[DllImport(\"someting\")]", "[UnmanagedCallersOnly]" } });
            string output = PublishForVariadicFunctionTests(info, config, aot);
            Assert.DoesNotMatch("warning\\sWASM0001.*Skipping.*Test::SomeFunction1.*because.*function\\spointer", output);
        }

        [Theory]
        [BuildAndRun(parameters: new object[] { new object[] {
                "with-hyphen",
                "with#hash-and-hyphen",
                "with.per.iod",
                "with🚀unicode#"
            } })]
        public async Task CallIntoLibrariesWithNonAlphanumericCharactersInTheirNames(Configuration config, bool aot, string[] libraryNames)
        {
            var extraItems = @"<NativeFileReference Include=""*.c"" />";
            string extraProperties = aot ? string.Empty : "<WasmBuildNative>true</WasmBuildNative>";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "abi", extraItems: extraItems, extraProperties: extraProperties);

            int baseArg = 10;
            GenerateSourceFiles(_projectDir, baseArg);
            bool isPublish = aot;
            (_, string output) = isPublish ?
                PublishProject(info, config, new PublishOptions(AOT: aot), isNativeBuild: true):
                BuildProject(info, config, new BuildOptions(AOT: aot), isNativeBuild: true);

            var runOptions = new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 42);
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

        private ProjectInfo PrepareProjectForVariadicFunction(Configuration config, bool aot, string prefix, string extraProperties = "")
        {
            string objectFilename = "variadic.o";
            extraProperties += "<AllowUnsafeBlocks>true</AllowUnsafeBlocks><_WasmDevel>true</_WasmDevel>";
            string extraItems = $"<NativeFileReference Include=\"{objectFilename}\" />";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, prefix, extraItems: extraItems, extraProperties: extraProperties);
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", objectFilename), Path.Combine(_projectDir, objectFilename));
            return info;
        }
    }
}
