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
                        test: output => {},
                        host: host, id: id);
        }

        [Theory]
        [BuildAndRun(aot: true, host: RunHost.None)]
        public void MonoAOTCross_WorksWithNoTrimming(BuildArgs buildArgs, string id)
        {
            // stop once `mono-aot-cross` part of the build is done
            string target = @"<Target Name=""StopAfterWasmAOT"" AfterTargets=""_WasmAotCompileApp"">
                <Error Text=""Stopping after AOT"" Condition=""'$(WasmBuildingForNestedPublish)' == 'true'"" />
            </Target>";

            string projectName = $"mono_aot_cross_{buildArgs.Config}_{buildArgs.AOT}";

            buildArgs = buildArgs with { ProjectName = projectName, ExtraBuildArgs = "-p:PublishTrimmed=false -v:n" };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties: "<WasmBuildNative>true</WasmBuildNative>", insertAtEnd: target);

            (_, string output) = BuildProject(
                                    buildArgs,
                                    id: id,
                                    new BuildProjectOptions(
                                        InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                                        DotnetWasmFromRuntimePack: false,
                                        ExpectSuccess: false));

            Assert.Contains("Stopping after AOT", output);
        }

        [Theory]
        [BuildAndRun(host: RunHost.None, aot: true)]
        public void IntermediateBitcodeToObjectFilesAreNotLLVMIR(BuildArgs buildArgs, string id)
        {
            string printFileTypeTarget = @"
                <Target Name=""PrintIntermediateFileType"" AfterTargets=""WasmNestedPublishApp"">
                    <Exec Command=""wasm-dis $(_WasmIntermediateOutputPath)System.Private.CoreLib.dll.o -o $(_WasmIntermediateOutputPath)wasm-dis-out.txt""
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

        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void BlazorWasm_CanRunMonoAOTCross_WithNoTrimming(string config)
        {
            string id = $"blazorwasm_{config}_aot";
            CreateBlazorWasmTemplateProject(id);

            // We don't want to emcc compile, and link ~180 assemblies!
            // So, stop once `mono-aot-cross` part of the build is done
            string target = @"<Target Name=""StopAfterWasmAOT"" AfterTargets=""_WasmAotCompileApp"">
                <Error Text=""Stopping after AOT"" Condition=""'$(WasmBuildingForNestedPublish)' == 'true'"" />
            </Target>
            ";
            AddItemsPropertiesToProject(Path.Combine(_projectDir!, $"{id}.csproj"),
                                        extraItems: null,
                                        extraProperties: null,
                                        atTheEnd: target);

            string publishLogPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}.binlog");
            CommandResult res = new DotNetCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir!)
                                        .ExecuteWithCapturedOutput("publish",
                                                                   $"-bl:{publishLogPath}",
                                                                   "-p:RunAOTCompilation=true",
                                                                   "-p:PublishTrimmed=false",
                                                                   $"-p:Configuration={config}");

            Assert.True(res.ExitCode != 0, "Expected publish to fail");
            Assert.Contains("Stopping after AOT", res.Output);
        }
    }
}
