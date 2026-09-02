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
        [TestCategory("mono")]
        public Task PublishRelaxedSimdMono(bool relaxedSimd) => PublishRelaxedSimd(relaxedSimd);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [TestCategory("coreclr")]
        public Task PublishRelaxedSimdCoreClr(bool relaxedSimd) => PublishRelaxedSimd(relaxedSimd);

        private async Task PublishRelaxedSimd(bool relaxedSimd)
        {
            Configuration config = Configuration.Debug;
            string relaxedSimdValue = relaxedSimd.ToString().ToLowerInvariant();
            string extraProperties = $"<WasmEnableRelaxedSimd>{relaxedSimdValue}</WasmEnableRelaxedSimd>";
            ProjectInfo info = CopyTestAsset(
                config,
                aot: false,
                TestAsset.WasmBasicTestApp,
                $"relaxed_simd_{relaxedSimdValue}",
                extraProperties: extraProperties);
            UpdateFile(
                Path.Combine("Common", "Program.cs"),
                IsCoreClrRuntime
                    ? GetCoreClrRelaxedSimdProgramText(relaxedSimdValue)
                    : GetMonoRelaxedSimdProgramText(relaxedSimdValue));
            ReplaceMainJsWithMinimalRunMain();

            PublishProject(info, config, isNativeBuild: relaxedSimd);

            RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42));
            string expectedOutput = IsCoreClrRuntime
                ? $"RelaxedSimd config: {(relaxedSimd ? "true" : "<null>")}"
                : $"RelaxedSimd.IsSupported: {relaxedSimd}";
            Assert.Contains(result.TestOutput, message => message.Contains(expectedOutput));
        }

        private static string GetCoreClrRelaxedSimdProgramText(string expectedConfigValue) => $$"""
            using System;

            public class TestClass
            {
                public static int Main()
                {
                    string configuredValue = AppContext.GetData(
                        "System.Runtime.Intrinsics.Wasm.RelaxedSimd.IsSupported") as string;
                    Console.WriteLine($"TestOutput -> RelaxedSimd config: {configuredValue ?? "<null>"}");

                    return configuredValue == {{(expectedConfigValue == "true" ? "\"true\"" : "null")}}
                        ? 42
                        : 1;
                }
            }
            """;

        private static string GetMonoRelaxedSimdProgramText(string expectedIsSupported) => $$"""
            using System;
            using System.Runtime.Intrinsics;
            using System.Runtime.Intrinsics.Wasm;

            public class TestClass
            {
                public static int Main()
                {
                    bool isSupported = RelaxedSimd.IsSupported;
                    Console.WriteLine($"TestOutput -> RelaxedSimd.IsSupported: {isSupported}");

                    if (isSupported != {{expectedIsSupported}})
                    {
                        return 1;
                    }

                    if (isSupported)
                    {
                        Vector128<int> result = RelaxedSimd.ConvertToInt32Native(
                            Vector128.Create(1.75f, -2.25f, 3.0f, -4.99f));

                        if (result != Vector128.Create(1, -2, 3, -4))
                        {
                            return 2;
                        }
                    }

                    return 42;
                }
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
