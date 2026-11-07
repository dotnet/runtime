// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmSIMDTests : WasmTemplateTestsBase
    {
        public WasmSIMDTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> MainMethodSimdTestData(bool aot, bool simd)
            => ConfigWithAOTData(aot)
                .Multiply(new object[] { simd })
                .Where(item => !(item.ElementAt(0) is Configuration config && config == Configuration.Debug && item.ElementAt(1) is bool aotValue && aotValue))
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(MainMethodSimdTestData), parameters: new object[] { /*aot*/ false, /* simd */ true })]
        public async Task Build_NoAOT_ShouldNotRelink(Configuration config, bool aot, bool simd)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "build_with_workload_no_aot");
            UpdateFile(Path.Combine("Common", "Program.cs"), s_simdProgramText);
            (string _, string output) = BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: $"-p:WasmEnableSIMD={simd}"));

            // Confirm that we didn't relink
            Assert.DoesNotContain("Compiling native assets with emcc", output);

            RunResult result = await RunForBuildWithDotnetRun(new BrowserRunOptions(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42)
            );

            Assert.Contains(result.TestOutput, m => m.Contains("<-2094756296, -2094756296, -2094756296, -2094756296>"));
            Assert.Contains(result.TestOutput, m => m.Contains("Hello, World!"));
        }

        [Theory]
        [MemberData(nameof(MainMethodSimdTestData), parameters: new object[] { /*aot*/ true, /* simd */ true })]
        [MemberData(nameof(MainMethodSimdTestData), parameters: new object[] { /*aot*/ false, /* simd */ true })]
        [MemberData(nameof(MainMethodSimdTestData), parameters: new object[] { /*aot*/ true, /* simd */ false })]
        [TestCategory("native"), TestCategory("mono")]
        public async Task PublishSIMD_AOT(Configuration config, bool aot, bool simd)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "simd_publish");
            UpdateFile(Path.Combine("Common", "Program.cs"), s_simdProgramText);
            (string _, string output) = PublishProject(info, config, new PublishOptions(ExtraMSBuildArgs: $"-p:WasmEnableSIMD={simd}", AOT: aot));

            RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42)
            );
            Assert.Contains(result.TestOutput, m => m.Contains("<-2094756296, -2094756296, -2094756296, -2094756296>"));
            Assert.Contains(result.TestOutput, m => m.Contains("Hello, World!"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [TestCategory("coreclr")]
        public async Task PublishRelaxedSimdCoreClr(bool relaxedSimd)
        {
            Configuration config = Configuration.Debug;
            string relaxedSimdValue = relaxedSimd.ToString().ToLowerInvariant();
            string extraProperties = $"<WasmEnableRelaxedSimd>{relaxedSimdValue}</WasmEnableRelaxedSimd><PublishReadyToRun>true</PublishReadyToRun>";
            ProjectInfo info = CopyTestAsset(
                config,
                aot: false,
                TestAsset.WasmBasicTestApp,
                $"relaxed_simd_{relaxedSimdValue}",
                extraProperties: extraProperties);
            UpdateFile(
                Path.Combine("Common", "Program.cs"),
                s_coreClrRelaxedSimdProgramText);
            ReplaceMainJsWithMinimalRunMain();

            PublishProject(info, config, isNativeBuild: relaxedSimd);

            RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42));
            Assert.Contains(result.TestOutput, m => m.Contains($"RelaxedSimd.IsSupported: {relaxedSimd}"));
            if (relaxedSimd)
            {
                Assert.Contains(result.TestOutput, m => m.Contains("ConvertToInt32Native: <1, -2, 3, -4>"));
                Assert.Contains(result.TestOutput, m => m.Contains("MultiplyAddEstimate: <7, 7, 7, 7>"));
            }
            else
            {
                Assert.Contains(result.TestOutput, m => m.Contains("ConvertToInt32Native: PlatformNotSupportedException"));
            }
        }

        private static string s_coreClrRelaxedSimdProgramText = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.Intrinsics;
            using System.Runtime.Intrinsics.Wasm;

            public class TestClass
            {
                public static int Main()
                {
                    Console.WriteLine($"TestOutput -> RelaxedSimd.IsSupported: {RelaxedSimd.IsSupported}");
                    try
                    {
                        Console.WriteLine($"TestOutput -> ConvertToInt32Native: {ConvertToInt32Native(Vector128.Create(1.75f, -2.25f, 3.0f, -4.99f))}");
                    }
                    catch (PlatformNotSupportedException)
                    {
                        Console.WriteLine("TestOutput -> ConvertToInt32Native: PlatformNotSupportedException");
                    }

                    if (RelaxedSimd.IsSupported)
                    {
                        Console.WriteLine($"TestOutput -> MultiplyAddEstimate: {MultiplyAddEstimate(Vector128.Create(2f), Vector128.Create(3f), Vector128.Create(1f))}");
                    }

                    return 42;
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Vector128<int> ConvertToInt32Native(Vector128<float> value) => RelaxedSimd.ConvertToInt32Native(value);

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Vector128<float> MultiplyAddEstimate(Vector128<float> left, Vector128<float> right, Vector128<float> addend) =>
                    RelaxedSimd.MultiplyAddEstimate(left, right, addend);
            }
            """;

        private static string s_simdProgramText = @"
            using System;
            using System.Runtime.Intrinsics;

            public class TestClass {
                public static int Main()
                {
                    var v1 = Vector128.Create(0x12345678);
                    var v2 = Vector128.Create(0x23456789);
                    var v3 = v1*v2;
                    Console.WriteLine($""TestOutput -> {v3}"");
                    Console.WriteLine(""TestOutput -> Hello, World!"");

                    return 42;
                }
            }";
    }
}
