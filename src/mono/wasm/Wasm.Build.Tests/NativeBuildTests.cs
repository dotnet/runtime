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
        public async Task SimpleNativeBuild(string config, bool aot)
        {
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot,
                "simple_native_build",
                extraProperties: "<WasmBuildNative>true</WasmBuildNative>");
            
            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

            bool isPublish = true;
            (string _, string buildOutput) = BuildProject(info,
                        new BuildOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild: true),
                            IsPublish: isPublish
                        ));
            await RunForPublishWithWebServer(new(config, ExpectedExitCode: 42));
        }

        [Theory]
        [BuildAndRun(aot: true)]
        public void AOTNotSupportedWithNoTrimming(string config, bool aot)
        {
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot,
                "mono_aot_cross",
                extraProperties: "<PublishTrimmed>false</PublishTrimmed>");
            
            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

            bool isPublish = true;
            (string _, string output) = BuildProject(info,
                        new BuildOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild: false),
                            IsPublish: isPublish,
                            ExpectSuccess: false
                        ));
            Assert.Contains("AOT is not supported without IL trimming", output);
        }

        [Theory]
        [BuildAndRun(config: "Release", aot: true)]
        public void IntermediateBitcodeToObjectFilesAreNotLLVMIR(string config, bool aot)
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

            bool isPublish = true;
            (string _, string output) = BuildProject(info,
                        new BuildOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild: false),
                            IsPublish: isPublish
                        ));

            if (!output.Contains("** wasm-dis exit code: 0"))
                throw new XunitException($"Expected to successfully run wasm-dis on System.Private.CoreLib.dll.o ."
                                            + " It might fail if it was incorrectly compiled to a bitcode file, instead of wasm.");
        }

        [Theory]
        [BuildAndRun(config: "Release", aot: true)]
        public void NativeBuildIsRequired(string config, bool aot)
        {
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot,
                "native_build",
                extraProperties: "<WasmBuildNative>false</WasmBuildNative><WasmSingleFileBundle>true</WasmSingleFileBundle>");

            bool isPublish = true;
            (string _, string output) = BuildProject(info,
                        new BuildOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild: false),
                            IsPublish: isPublish,
                            ExpectSuccess: false
                        ));

            Assert.Contains("WasmBuildNative is required", output);
        }
    }
}
