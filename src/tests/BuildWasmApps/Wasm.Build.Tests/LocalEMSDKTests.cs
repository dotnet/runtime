// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class LocalEMSDKTests : BuildTestBase
    {
        public LocalEMSDKTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : base(output, buildContext)
        {}

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsNotUsingWorkloads))]
        [BuildAndRun(aot: true, host: RunHost.None, parameters: new object[]
                        { "", "error :.*emscripten.*required for AOT" })]
        [BuildAndRun(aot: true, host: RunHost.None, parameters: new object[]
                        { "/non-existant/foo", "error.*\\(EMSDK_PATH\\)=/non-existant/foo.*required for AOT" })]
        public void AOT_ErrorWhenMissingEMSDK(BuildArgs buildArgs, string emsdkPath, string errorPattern, string id)
        {
            string projectName = $"missing_emsdk";
            buildArgs = buildArgs with {
                            ProjectName = projectName,
                            ExtraBuildArgs = $"/p:EMSDK_PATH={emsdkPath}"
            };
            buildArgs = ExpandBuildArgs(buildArgs);

            (_, string buildOutput) = BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                        id: id,
                        expectSuccess: false);

            Assert.Matches(errorPattern, buildOutput);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsNotUsingWorkloads))]
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
    }
 }
