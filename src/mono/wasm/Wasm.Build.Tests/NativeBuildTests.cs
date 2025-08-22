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
            UpdateBrowserMainJs();

            (string _, string buildOutput) = PublishProject(info, config, isNativeBuild: true);
            await RunForPublishWithWebServer(new BrowserRunOptions(config, ExpectedExitCode: 42));
        }

        [Theory]
        [BuildAndRun(aot: true)]
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

        [Fact, TestCategory("bundler-friendly")]
        public async Task ZipArchiveInteropTest()
        {
            Configuration config = Configuration.Debug;
            ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "ZipArchiveInteropTest", extraProperties: "<WasmBuildNative>true</WasmBuildNative>");
            BuildProject(info, config);
            RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "ZipArchiveInteropTest"));
            Assert.Collection(
                result.TestOutput,
                m => Assert.Equal("Zip file created successfully.", m)
            );
        }
    }
}
