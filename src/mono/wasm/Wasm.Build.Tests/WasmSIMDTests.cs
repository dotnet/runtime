// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmSIMDTests : WasmBuildAppBase
    {
        public WasmSIMDTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void BuildWithSIMD_NoAOT_ShouldRelink(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"sim_with_workload_no_aot";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, "<WasmEnableSIMD>true</WasmEnableSIMD>");

            (_, string output) = BuildProject(buildArgs,
                                    id: id,
                                    new BuildProjectOptions(
                                        InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_simdProgramText),
                                        Publish: false,
                                        DotnetWasmFromRuntimePack: false));

            if (!_buildContext.TryGetBuildFor(buildArgs, out _))
            {
                // Check if this is not a cached build
                Assert.Contains("Compiling native assets with excc", output);
            }

            RunAndTestWasmApp(buildArgs,
                                extraXHarnessArgs: host == RunHost.NodeJS ? "--engine-arg=--experimental-wasm-simd" : "",
                                expectedExitCode: 42,
                                test: output =>
                                {
                                    Assert.Contains("<-2094756296, -2094756296, -2094756296, -2094756296>", output);
                                    Assert.Contains("Hello, World!", output);
                                }, host: host, id: id);
        }

        [Theory]
        // https://github.com/dotnet/runtime/issues/75044 - disabled for V8, and NodeJS
        //[MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true, RunHost.Chrome })]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void PublishWithSIMD_AOT(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"sim_with_workload_aot";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, "<WasmEnableSIMD>true</WasmEnableSIMD>");

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_simdProgramText),
                                DotnetWasmFromRuntimePack: false));

            RunAndTestWasmApp(buildArgs,
                                extraXHarnessArgs: host == RunHost.NodeJS ? "--engine-arg=--experimental-wasm-simd" : "",
                                expectedExitCode: 42,
                                test: output =>
                                {
                                    Assert.Contains("<-2094756296, -2094756296, -2094756296, -2094756296>", output);
                                    Assert.Contains("Hello, World!", output);
                                }, host: host, id: id);
        }

        [Theory, TestCategory("no-workload")]
        [InlineData("Debug", /*aot*/true, /*publish*/true)]
        [InlineData("Debug", /*aot*/false, /*publish*/false)]
        [InlineData("Debug", /*aot*/false, /*publish*/true)]
        [InlineData("Release", /*aot*/true, /*publish*/true)]
        [InlineData("Release", /*aot*/false, /*publish*/false)]
        [InlineData("Release", /*aot*/false, /*publish*/true)]
        public void BuildWithSIMDNeedsWorkload(string config, bool aot, bool publish)
        {
            string id = Path.GetRandomFileName();
            string projectName = $"simd_no_workload_{config}_aot_{aot}";
            BuildArgs buildArgs = new
            (
                ProjectName: projectName,
                Config: config,
                AOT: aot,
                ProjectFileContents: "placeholder",
                ExtraBuildArgs: string.Empty
            );

            string extraProperties = """
            <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
            <WasmEnableSIMD>true</WasmEnableSIMD>
            """;
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties);

            (_, string output) = BuildProject(buildArgs,
                                    id: id,
                                    new BuildProjectOptions(
                                        InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_simdProgramText),
                                        Publish: publish,
                                        ExpectSuccess: false,
                                        UseCache: false));
            Assert.Contains("following workloads must be installed: wasm-tools", output);
        }

        private static string s_simdProgramText = @"
            using System;
            using System.Runtime.Intrinsics;

            public class TestClass {
                public static int Main()
                {
                    var v1 = Vector128.Create(0x12345678);
                    var v2 = Vector128.Create(0x23456789);
                    var v3 = v1*v2;
                    Console.WriteLine(v3);
                    Console.WriteLine(""Hello, World!"");

                    return 42;
                }
            }";
    }
}
