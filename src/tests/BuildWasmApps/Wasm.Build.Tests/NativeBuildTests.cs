// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class NativeBuildTests : BuildTestBase
    {
        public NativeBuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        // TODO:     - check dotnet.wasm, js have changed
        //           - icall? pinvoke?
        //           - test defaults
        //

        [Theory]
        [BuildAndRun]
        public void SimpleNativeBuild(BuildArgs buildArgs, RunHost host, string id)
            => NativeBuild("simple_native_build", s_mainReturns42, buildArgs, host, id);

        private void NativeBuild(string projectNamePrefix, string projectContents, BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"{projectNamePrefix}_{buildArgs.Config}_{buildArgs.AOT}";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties: "<WasmBuildNative>true</WasmBuildNative>");

            BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), projectContents),
                        dotnetWasmFromRuntimePack: false,
                        id: id);

            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42,
                        test: output => {},
                        host: host, id: id);
        }

        [Theory]
        [BuildAndRun(host: RunHost.None, aot: true)]
        public void IntermediateBitcodeToObjectFilesAreNotLLVMIR(BuildArgs buildArgs, string id)
        {
            string printFileTypeTarget = @"
                <Target Name=""PrintIntermediateFileType"" AfterTargets=""WasmBuildApp"">
                    <Exec Command=""wasm-dis $(_WasmIntermediateOutputPath)System.Private.CoreLib.dll.o -o $(_WasmIntermediateOutputPath)wasm-dis-out.txt""
                          ConsoleToMSBuild=""true""
                          EnvironmentVariables=""@(EmscriptenEnvVars)""
                          IgnoreExitCode=""true"">

                        <Output TaskParameter=""ExitCode"" PropertyName=""ExitCode"" />
                    </Exec>

                    <Message Text=""wasm-dis exit code: $(ExitCode)"" Importance=""High"" />
                </Target>
                ";
            string projectName = $"bc_to_o_{buildArgs.Config}";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, insertAtEnd: printFileTypeTarget);

            (_, string output) = BuildProject(buildArgs,
                                    initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                                    dotnetWasmFromRuntimePack: false,
                                    id: id);

            if (!output.Contains("wasm-dis exit code: 0"))
                throw new XunitException($"Expected to successfully run wasm-dis on System.Private.CoreLib.dll.o ."
                                            + " It might fail if it was incorrectly compiled to a bitcode file, instead of wasm.");
        }
    }
}
