// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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

        public static IEnumerable<object?[]> MainMethodSimdTestData(bool aot, RunHost host, bool simd)
            => ConfigWithAOTData(aot, extraArgs: $"-p:WasmEnableSIMD={simd}")
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(MainMethodSimdTestData), parameters: new object[] { /*aot*/ false, RunHost.All, true /* simd */ })]
        [MemberData(nameof(MainMethodSimdTestData), parameters: new object[] { /*aot*/ false, RunHost.All, false /* simd */ })]
        public void Build_NoAOT_ShouldNotRelink(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"build_with_workload_no_aot";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs);

            (_, string output) = BuildProject(buildArgs,
                                    id: id,
                                    new BuildProjectOptions(
                                        InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_simdProgramText),
                                        Publish: false,
                                        DotnetWasmFromRuntimePack: true));

            // Confirm that we didn't relink
            Assert.DoesNotContain("Compiling native assets with emcc", output);

            RunAndTestWasmApp(buildArgs,
                                extraXHarnessArgs: host == RunHost.NodeJS ? "--engine-arg=--experimental-wasm-simd --engine-arg=--experimental-wasm-eh" : "",
                                expectedExitCode: 42,
                                test: output =>
                                {
                                    Assert.Contains("<-2094756296, -2094756296, -2094756296, -2094756296>", output);
                                    Assert.Contains("Hello, World!", output);
                                }, host: host, id: id);
        }

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void PublishWithSIMD_AOT(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"simd_with_workload_aot";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, "<WasmEnableSIMD>true</WasmEnableSIMD>");

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_simdProgramText),
                                DotnetWasmFromRuntimePack: false));

            RunAndTestWasmApp(buildArgs,
                                extraXHarnessArgs: host == RunHost.NodeJS ? "--engine-arg=--experimental-wasm-simd --engine-arg=--experimental-wasm-eh" : "",
                                expectedExitCode: 42,
                                test: output =>
                                {
                                    Assert.Contains("<-2094756296, -2094756296, -2094756296, -2094756296>", output);
                                    Assert.Contains("Hello, World!", output);
                                }, host: host, id: id);
        }

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void PublishWithoutSIMD_AOT(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"nosimd_with_workload_aot";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, "<WasmEnableSIMD>false</WasmEnableSIMD>");

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_simdProgramText),
                                DotnetWasmFromRuntimePack: false));

            RunAndTestWasmApp(buildArgs,
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
