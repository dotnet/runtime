// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
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

        [Fact]
        [TestCategory("mono")]
        public Task RuntimeAsyncMethodIsRejected()
            => TestRuntimeAsyncMethod(aot: false);

        [Fact]
        [TestCategory("native-mono")]
        public Task RuntimeAsyncMethodIsRejected_AOT()
            => TestRuntimeAsyncMethod(aot: true);

        private async Task TestRuntimeAsyncMethod(bool aot)
        {
            Configuration config = aot ? Configuration.Release : Configuration.Debug;
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "runtime_async_dependency",
                extraProperties: aot ? "<WasmStripILAfterAOT>true</WasmStripILAfterAOT>" : "",
                extraItems: """<ProjectReference Include="..\Library\Library.csproj" />""");
            UpdateFile(Path.Combine("..", "Library", "Library.csproj"), new Dictionary<string, string>
            {
                { "</PropertyGroup>", "<Features>$(Features);runtime-async=on</Features></PropertyGroup>" }
            });
            UpdateFile(Path.Combine("..", "Library", "Library.cs"), """
                using System;
                using System.Threading.Tasks;

                public class AsyncDependency
                {
                    public static int Ordinary() => 42;

                    public static async Task<int> Unsupported()
                    {
                        Console.WriteLine("TestOutput -> unsupported body ran");
                        int value = 1;
                        return await Task.FromResult(Read(ref value));
                    }

                    private static int Read(ref int value) => value;
                }
                """);
            UpdateFile(Path.Combine("Common", "Program.cs"), """
                using System;

                public class TestClass
                {
                    public static int Main()
                    {
                        Console.WriteLine($"TestOutput -> ordinary result: {AsyncDependency.Ordinary()}");
                        try
                        {
                            Console.WriteLine(AsyncDependency.Unsupported().GetAwaiter().GetResult());
                            return 1;
                        }
                        catch (PlatformNotSupportedException ex)
                        {
                            Console.WriteLine($"TestOutput -> {ex.Message}");
                            return 0;
                        }
                    }
                }
                """);
            if (aot)
            {
                PublishProject(info, config, new PublishOptions(AOT: true));
                string strippedLibrary = Path.Combine(GetObjDir(config), "wasm", "for-publish", "stripped", "Library.dll");
                AssertRuntimeAsyncMethod(strippedLibrary, expectStripped: true);
            }
            else
            {
                BuildProject(info, config);
            }

            string libraryPath = Path.Combine(_projectDir, "..", "Library", "bin", config.ToString(), DefaultTargetFramework, "Library.dll");
            AssertRuntimeAsyncMethod(libraryPath, expectStripped: false);

            BrowserRunOptions options = new(config, TestScenario: "DotnetRun", ExpectedExitCode: 0);
            RunResult result = aot
                ? await RunForPublishWithWebServer(options)
                : await RunForBuildWithDotnetRun(options);
            Assert.Contains("ordinary result: 42", result.TestOutput);
            Assert.DoesNotContain("unsupported body ran", result.TestOutput);
            Assert.Contains(result.TestOutput, message =>
                message.Contains("Mono does not support runtime-async method", StringComparison.Ordinal) &&
                message.Contains("AsyncDependency:Unsupported", StringComparison.Ordinal) &&
                message.Contains("assembly 'Library'", StringComparison.Ordinal));
        }

        private static void AssertRuntimeAsyncMethod(string assemblyPath, bool expectStripped)
        {
            using var stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);
            MetadataReader metadata = peReader.GetMetadataReader();
            MethodDefinitionHandle handle = metadata.MethodDefinitions.Single(candidate =>
                metadata.GetString(metadata.GetMethodDefinition(candidate).Name) == "Unsupported");
            MethodDefinition method = metadata.GetMethodDefinition(handle);
            Assert.True((method.ImplAttributes & MethodImplAttributes.Async) != 0);
            Assert.Equal(expectStripped, peReader.GetMethodBody(method.RelativeVirtualAddress).GetILContent().IsEmpty);
        }

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true })]
        [TestCategory("native-mono")]
        public async Task TopLevelMain_AOT(Configuration config, bool aot)
            => await TestMain("top_level",
                    @"System.Console.WriteLine(""Hello, World!""); return await System.Threading.Tasks.Task.FromResult(42);",
                    config, aot);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false })]
        public async Task TopLevelMain(Configuration config, bool aot)
            => await TestMain("top_level",
                    @"System.Console.WriteLine(""Hello, World!""); return await System.Threading.Tasks.Task.FromResult(42);",
                    config, aot);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true })]
        [TestCategory("native-mono")]
        public async Task AsyncMain_AOT(Configuration config, bool aot)
            => await TestMain("async_main", @"
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
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false })]
        public async Task AsyncMain(Configuration config, bool aot)
            => await TestMain("async_main", @"
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
        [TestCategory("native-mono")]
        public async Task NonAsyncMain_AOT(Configuration config, bool aot)
            => await TestMain("non_async_main", @"
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
        public async Task NonAsyncMain(Configuration config, bool aot)
            => await TestMain("non_async_main", @"
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
        public async Task ExceptionFromMain(Configuration config, bool aot)
            => await TestMain("main_exception", """
                using System;
                using System.Threading.Tasks;

                public class TestClass {
                    public static int Main() => throw new Exception("MessageFromMyException");
                }
                """, config, aot, expectedExitCode: 1, expectedOutput: "MessageFromMyException");

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
        [TestCategory("native-mono")]
        public async Task Bug49588_RegressionTest_AOT(Configuration config, bool aot)
            => await TestMain("bug49588_aot", s_bug49588_ProgramCS, config, aot);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false })]
        [TestCategory("native-mono")]
        public async Task Bug49588_RegressionTest_NativeRelinking(Configuration config, bool aot)
            => await TestMain("bug49588_native_relinking", s_bug49588_ProgramCS, config, aot,
                        extraArgs: "-p:WasmBuildNative=true",
                        isNativeBuild: true);

        [Theory]
        [BuildAndRun]
        public async Task PropertiesFromRuntimeConfigJson(Configuration config, bool aot)
            => await TestMain("runtime_config_json",
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
        public async Task PropertiesFromCsproj(Configuration config, bool aot)
            => await TestMain("csproj_properties",
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
