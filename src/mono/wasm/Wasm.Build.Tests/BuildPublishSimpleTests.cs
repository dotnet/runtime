// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Wasm.Build.NativeRebuild.Tests;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests
{
    public class BuildPublishSimpleTests : NativeRebuildTestsBase
    {
        public BuildPublishSimpleTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun(host: RunHost.Chrome, aot: false)]
        public void OnlyRelink(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = GetTestProjectPath(
                prefix: "publish_with_relink", config: buildArgs.Config);

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties: "<_WasmDevel>true</_WasmDevel><WasmBuildNative>true</WasmBuildNative>");

            (_, string output) = BuildProject(buildArgs,
                                    id,
                                    new BuildProjectOptions(
                                        InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                                        DotnetWasmFromRuntimePack: false,
                                        CreateProject: true,
                                        Publish: true));
            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
        }

        [Theory]
        [BuildAndRun(host: RunHost.Chrome, aot: true)]
        public void OnlyAOT(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = GetTestProjectPath(
                prefix: "publish_with_AOT", config: buildArgs.Config);

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties: "<_WasmDevel>true</_WasmDevel>");

            (_, string output) = BuildProject(buildArgs,
                                    id,
                                    new BuildProjectOptions(
                                        InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                                        DotnetWasmFromRuntimePack: false,
                                        CreateProject: true,
                                        Publish: true));
            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
        }
    }
}
