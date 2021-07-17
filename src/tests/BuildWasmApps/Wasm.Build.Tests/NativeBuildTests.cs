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
