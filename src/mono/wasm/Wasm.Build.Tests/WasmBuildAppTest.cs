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
        public void TopLevelMain(Configuration config, bool aot)
            => TestMain("top_level",
                    @"System.Console.WriteLine(""Hello, World!""); return await System.Threading.Tasks.Task.FromResult(42);",
                    config, aot);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true })]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false })]
        public void AsyncMain(Configuration config, bool aot)
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
        public void NonAsyncMain(Configuration config, bool aot)
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
        public void ExceptionFromMain(Configuration config, bool aot)
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
        public void Bug49588_RegressionTest_AOT(Configuration config, bool aot)
            => TestMain("bug49588_aot", s_bug49588_ProgramCS, config, aot);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false })]
        public void Bug49588_RegressionTest_NativeRelinking(Configuration config, bool aot)
            => TestMain("bug49588_native_relinking", s_bug49588_ProgramCS, config, aot,
                        extraArgs: "-p:WasmBuildNative=true",
                        isNativeBuild: true);

        [Theory]
        [BuildAndRun]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/97449")]
        public void PropertiesFromRuntimeConfigJson(Configuration config, bool aot)
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/97449")]
        public void PropertiesFromCsproj(Configuration config, bool aot)
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
}
