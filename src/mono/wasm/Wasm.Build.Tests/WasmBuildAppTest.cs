// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmBuildAppTest : WasmBuildAppBase
    {
        // similar to MainWithArgsTests.cs, consider merging
        public WasmBuildAppTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : base(output, buildContext)
        {}

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true })]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false })]
        public void TopLevelMain(string config, bool aot)
            => TestMain("top_level",
                    @"System.Console.WriteLine(""Hello, World!""); return await System.Threading.Tasks.Task.FromResult(42);",
                    config, aot);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true })]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false })]
        public void AsyncMain(string config, bool aot)
            => TestMain("async_main", @"
            using System;
            using System.Threading.Tasks;

            public class TestClass {
                public static async Task<int> Main()
                {
                    Console.WriteLine(""Hello, World!"");
                    return await Task.FromResult(42);
                }
            }", config, aot);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true })]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false })]
        public void NonAsyncMain(string config, bool aot)
            => TestMain("non_async_main", @"
                using System;
                using System.Threading.Tasks;

                public class TestClass {
                    public static int Main()
                    {
                        Console.WriteLine(""Hello, World!"");
                        return 42;
                    }
                }", config, aot);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false })]
        public void ExceptionFromMain(string config, bool aot)
            => TestMain("main_exception", """
                using System;
                using System.Threading.Tasks;

                public class TestClass {
                    public static int Main() => throw new Exception("MessageFromMyException");
                }
                """, config, aot, expectedExitCode: 1, expectedOutput: "Error: MessageFromMyException");

        private static string s_bug49588_ProgramCS = @"
            using System;
            public class TestClass {
                public static int Main()
                {
                    Console.WriteLine($""tc: {Environment.TickCount}, tc64: {Environment.TickCount64}"");

                    // if this gets printed, then we didn't crash!
                    Console.WriteLine(""Hello, World!"");
                    return 42;
                }
            }";

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true })]
        public void Bug49588_RegressionTest_AOT(string config, bool aot)
            => TestMain("bug49588_aot", s_bug49588_ProgramCS, config, aot);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false })]
        public void Bug49588_RegressionTest_NativeRelinking(string config, bool aot)
            => TestMain("bug49588_native_relinking", s_bug49588_ProgramCS, config, aot,
                        extraArgs: "-p:WasmBuildNative=true",
                        isNativeBuild: true);

        [Theory]
        [BuildAndRun]
        // Issue: we cannot read from the config file
        public void PropertiesFromRuntimeConfigJson(string config, bool aot)
            => TestMain("runtime_config_json",
                        @"
                        using System;
                        using System.Runtime.CompilerServices;

                        var config = AppContext.GetData(""test_runtimeconfig_json"");
                        Console.WriteLine ($""test_runtimeconfig_json: {(string)config}"");
                        return 42;
                        ",
                        config,
                        aot,
                        runtimeConfigContents: @"
                            },
                            ""configProperties"": {
                            ""abc"": ""4"",
                            ""test_runtimeconfig_json"": ""25""
                            }
                        }",
                        expectedOutput: "test_runtimeconfig_json: 25");

        [Theory]
        [BuildAndRun]
        // Issue: we cannot read from the config file
        public void PropertiesFromCsproj(string config, bool aot)
            => TestMain("csproj_properties",
                        @"
                        using System;
                        using System.Runtime.CompilerServices;

                        var config = AppContext.GetData(""System.Threading.ThreadPool.MaxThreads"");
                        Console.WriteLine ($""System.Threading.ThreadPool.MaxThreads: {(string)config}"");
                        return 42;
                        ",
                        config,
                        aot,
                        extraArgs: "-p:ThreadPoolMaxThreads=20",
                        expectedOutput: "System.Threading.ThreadPool.MaxThreads: 20");
    }

    public class WasmBuildAppBase : WasmTemplateTestsBase
    {
        public static IEnumerable<object?[]> MainMethodTestData(bool aot)
            => ConfigWithAOTData(aot)
                .Where(item => !(item.ElementAt(0) is string config && config == "Debug" && item.ElementAt(1) is bool aotValue && aotValue))
                .UnwrapItemsAsArrays();

        public WasmBuildAppBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : base(output, buildContext)
        {
        }

        protected async void TestMain(string projectName,
              string programText,
              string config,
              bool aot,
              bool isNativeBuild = false,
              int expectedExitCode = 42,
              string expectedOutput = "Hello, World!",
              string runtimeConfigContents = "",
              params string[] extraArgs)
        {
            ProjectInfo info = CopyTestAsset(config, aot, "WasmBasicTestApp", "TestMain", "App");
            UpdateFile(Path.Combine("Common", "Program.cs"), programText);
            if (!string.IsNullOrEmpty(runtimeConfigContents))
            {
                UpdateFile("runtimeconfig.template.json", new Dictionary<string, string> { {  "}\n}", runtimeConfigContents } });
            }
            bool isPublish = true;
            BuildTemplateProject(info,
                new BuildProjectOptions(
                    info.Configuration,
                    info.ProjectName,
                    BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                    ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish, isNativeBuild),
                    IsPublish: isPublish
                    ),
                extraArgs
            );
            RunResult result = await RunForPublishWithWebServer(new(
                info.Configuration,
                TestScenario: "TestMain",
                ExpectedExitCode: expectedExitCode)
            );
            Assert.Contains(result.ConsoleOutput, m => m.Contains(expectedOutput));
        }
    }
}
