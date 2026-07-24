// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class NativeBuildTests : WasmTemplateTestsBase
    {
        public NativeBuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun(aot: false)]
        public async Task SimpleNativeBuild(Configuration config, bool aot)
        {
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot,
                "simple_native_build",
                extraProperties: "<WasmBuildNative>true</WasmBuildNative>");
            
            UpdateBrowserProgramFile();
            // The replacement program does not use JS interop, so the JS interop assembly would be
            // linked away by the trimmer (CoreCLR-Wasm) and the template main.js (which calls
            // getAssemblyExports) would fail at startup.
            ReplaceMainJsWithMinimalRunMain();

            (string _, string buildOutput) = PublishProject(info, config, isNativeBuild: true);
            await RunForPublishWithWebServer(new BrowserRunOptions(config, ExpectedExitCode: 42));
        }

        [Theory]
        [BuildAndRun(aot: false)]
        [TestCategory("native-mono")]
        [SkipOnPlatform(TestPlatforms.AnyUnix, "The cmd.exe quoting behavior this covers is Windows-specific.")]
        public async Task NativeBuildWithSpecialCharsInTempPath(Configuration config, bool aot)
        {
            // Regression test for https://github.com/dotnet/runtime/issues/120327.
            // Native compilation runs the compiler through a temporary batch file created under the
            // temp directory. Windows user profile names can contain parentheses (e.g. "John(US)"),
            // which puts parentheses in %TEMP%. `cmd /c "<path>"` then stripped the quotes around
            // that path and mis-parsed it at the first '(', failing the native build with
            // "'C:\Users\John' is not recognized as an internal or external command".
            // The unicode chars additionally cover the UTF-8 (chcp 65001) handling in the same
            // RunShellCommand path, which exists so non-ASCII (e.g. GB18030) temp/user paths work.
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot,
                "parens_temp",
                extraProperties: "<WasmBuildNative>true</WasmBuildNative>");

            UpdateBrowserProgramFile();
            ReplaceMainJsWithMinimalRunMain();

            string tempWithParens = Path.Combine(BuildEnvironment.TmpPath, $"tmp ({GetRandomId()}) {s_unicodeChars}");
            Directory.CreateDirectory(tempWithParens);
            var envVars = new Dictionary<string, string>
            {
                ["TMP"] = tempWithParens,
                ["TEMP"] = tempWithParens,
            };

            PublishProject(info, config, new PublishOptions(ExtraBuildEnvironmentVariables: envVars), isNativeBuild: true);
            await RunForPublishWithWebServer(new BrowserRunOptions(config, ExpectedExitCode: 42));
        }

        [Theory]
        [BuildAndRun(aot: true)]
        [TestCategory("native-mono")]
        public void AOTNotSupportedWithNoTrimming(Configuration config, bool aot)
        {
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot,
                "mono_aot_cross",
                extraProperties: "<PublishTrimmed>false</PublishTrimmed>");
            
            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

            (string _, string output) = PublishProject(info, config, new PublishOptions(ExpectSuccess: false, AOT: aot));
            Assert.Contains("AOT is not supported without IL trimming", output);
        }

        [Theory]
        [BuildAndRun(config: Configuration.Release, aot: true)]
        [TestCategory("native-mono")]
        public void IntermediateBitcodeToObjectFilesAreNotLLVMIR(Configuration config, bool aot)
        {
            string printFileTypeTarget = @"
                <Target Name=""PrintIntermediateFileType"" AfterTargets=""WasmNestedPublishApp"">
                    <Exec Command=""wasm-dis &quot;$(_WasmIntermediateOutputPath)System.Private.CoreLib.dll.o&quot; -o &quot;$(_WasmIntermediateOutputPath)wasm-dis-out.txt&quot;""
                          EnvironmentVariables=""@(EmscriptenEnvVars)""
                          IgnoreExitCode=""true"">

                        <Output TaskParameter=""ExitCode"" PropertyName=""ExitCode"" />
                    </Exec>

                    <Message Text=""
                    ** wasm-dis exit code: $(ExitCode)
                    "" Importance=""High"" />
                </Target>
                ";
            
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot,
                "bc_to_o",
                insertAtEnd: printFileTypeTarget);

            (string _, string output) = PublishProject(info, config, new PublishOptions(AOT: aot));
            if (!output.Contains("** wasm-dis exit code: 0"))
                throw new XunitException($"Expected to successfully run wasm-dis on System.Private.CoreLib.dll.o ."
                                            + " It might fail if it was incorrectly compiled to a bitcode file, instead of wasm.");
        }

        [Theory]
        [BuildAndRun(config: Configuration.Release, aot: true)]
        [TestCategory("native-mono")]
        public void NativeBuildIsRequired(Configuration config, bool aot)
        {
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot,
                "native_build",
                extraProperties: "<WasmBuildNative>false</WasmBuildNative><WasmSingleFileBundle>true</WasmSingleFileBundle>");

            (string _, string output) = PublishProject(info, config, new PublishOptions(ExpectSuccess: false, AOT: aot));
            Assert.Contains("WasmBuildNative is required", output);
        }

        [Fact]
        public async Task ZipArchiveInteropTest()
        {
            Configuration config = Configuration.Debug;
            ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "ZipArchiveInteropTest", extraProperties: "<WasmBuildNative>true</WasmBuildNative>");
            BuildProject(info, config, new BuildOptions(AssertAppBundle: false));
            BuildPaths paths = GetBuildPaths(config, forPublish: false);
            string pinvokeTableFileName = IsCoreClrRuntime ? "callhelpers-pinvoke.cpp" : "pinvoke-table.h";
            Assert.DoesNotContain("System_Security_Cryptography", File.ReadAllText(Path.Combine(paths.ObjWasmDir, pinvokeTableFileName)));
            RunResult result = await RunForBuildWithDotnetRun(new BrowserRunOptions(config, TestScenario: "ZipArchiveInteropTest"));
            Assert.Collection(
                result.TestOutput,
                m => Assert.Equal("Zip file created successfully.", m)
            );
        }
    }
}
