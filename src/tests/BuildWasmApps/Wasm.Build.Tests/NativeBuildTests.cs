// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

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

        [Theory]
        [BuildAndRun(host: RunHost.None, parameters: new object[]
                        { "", "error.*emscripten.*required for building native files" })]
        [BuildAndRun(host: RunHost.None, parameters: new object[]
                        { "/non-existant/foo", "error.*\\(EMSDK_PATH\\)=/non-existant/foo.*required for building native files" })]
        public void Relinking_ErrorWhenMissingEMSDK(BuildArgs buildArgs, string emsdkPath, string errorPattern, string id)
        {
            string projectName = $"simple_native_build";
            buildArgs = buildArgs with {
                            ProjectName = projectName,
                            ExtraBuildArgs = $"/p:EMSDK_PATH={emsdkPath}"
            };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties: "<WasmBuildNative>true</WasmBuildNative>");

            (_, string buildOutput) = BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                        id: id,
                        expectSuccess: false);

            Assert.Matches(errorPattern, buildOutput);
        }

        private void NativeBuild(string projectNamePrefix, string projectContents, BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"{projectNamePrefix}_{buildArgs.Config}_{buildArgs.AOT}";

            buildArgs = buildArgs with { ProjectName = projectName, ProjectFileContents = projectContents };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties: "<WasmBuildNative>true</WasmBuildNative>");
            Console.WriteLine ($"-- args: {buildArgs}, name: {projectName}");

            BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), projectContents),
                        dotnetWasmFromRuntimePack: false,
                        id: id);

            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42,
                        test: output => {},
                        host: host, id: id);
        }
    }
}
