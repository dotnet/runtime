// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class NativeBuildTests : TestMainJsTestBase
    {
        public NativeBuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun]
        public void SimpleNativeBuild(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"simple_native_build_{buildArgs.Config}_{buildArgs.AOT}";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties: "<WasmBuildNative>true</WasmBuildNative>");

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                                DotnetWasmFromRuntimePack: false));

            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42,
                        test: output => { },
                        host: host, id: id);
        }

        [Theory]
        [BuildAndRun(aot: true, host: RunHost.None)]
        public void AOTNotSupportedWithNoTrimming(BuildArgs buildArgs, string id)
        {
            string projectName = $"mono_aot_cross_{buildArgs.Config}_{buildArgs.AOT}";

            buildArgs = buildArgs with { ProjectName = projectName, ExtraBuildArgs = "-p:PublishTrimmed=false" };
            buildArgs = ExpandBuildArgs(buildArgs);

            (_, string output) = BuildProject(
                                    buildArgs,
                                    id: id,
                                    new BuildProjectOptions(
                                        InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                                        DotnetWasmFromRuntimePack: false,
                                        ExpectSuccess: false));

            Assert.Contains("AOT is not supported without IL trimming", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.None, aot: true)]
        public void IntermediateBitcodeToObjectFilesAreNotLLVMIR(BuildArgs buildArgs, string id)
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
            string projectName = $"bc_to_o_{buildArgs.Config}";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, insertAtEnd: printFileTypeTarget);

            (_, string output) = BuildProject(buildArgs,
                                    id: id,
                                    new BuildProjectOptions(
                                        InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                                        DotnetWasmFromRuntimePack: false));

            if (!output.Contains("** wasm-dis exit code: 0"))
                throw new XunitException($"Expected to successfully run wasm-dis on System.Private.CoreLib.dll.o ."
                                            + " It might fail if it was incorrectly compiled to a bitcode file, instead of wasm.");
        }

    }
}
