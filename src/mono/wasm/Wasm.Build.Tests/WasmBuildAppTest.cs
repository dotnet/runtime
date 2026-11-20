// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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
        [InlineData(false)]
        [InlineData(true)]
        [TestCategory("mono")]
        public async Task RuntimeAsyncAssemblyIsRejected(bool runtimeAsync)
        {
            const string diagnostic = "The assembly 'WasmBasicTestApp' contains runtime-async methods, which are not supported by Mono.";
            const string mainOutput = "TestOutput -> main ran";
            Configuration config = Configuration.Debug;
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "runtime_async",
                extraProperties: runtimeAsync ? "<Features>$(Features);runtime-async=on</Features>" : "");
            UpdateFile(Path.Combine("Common", "Program.cs"), $$"""
                using System;
                using System.Threading.Tasks;

                public class TestClass
                {
                    public static int Main()
                    {
                        Console.WriteLine("{{mainOutput}}");
                        return 0;
                    }

                    public static async Task<int> Unused() => await Task.FromResult(1);
                }
                """);
            BuildProject(info, config);

            string assemblyPath = Path.Combine(GetObjDir(config), $"{info.ProjectName}.dll");
            AssertRuntimeAsyncFlag(assemblyPath, runtimeAsync);

            var mainRan = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var rejected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var pageError = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await RunForBuildWithDotnetRun(new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: null)
            {
                OnConsoleMessage = (_, message) =>
                {
                    if (message.Contains(mainOutput, StringComparison.Ordinal))
                        mainRan.TrySetResult();
                    if (message.Contains(diagnostic, StringComparison.Ordinal))
                        rejected.TrySetResult();
                },
                OnErrorMessage = message =>
                {
                    if (message.StartsWith("PageError:", StringComparison.Ordinal))
                        pageError.TrySetResult();
                },
                ExecuteAfterLoaded = async (_, page) =>
                {
                    await Task.WhenAny(mainRan.Task, pageError.Task).WaitAsync(TimeSpan.FromSeconds(60));
                    await page.EvaluateAsync("console.log('WASM EXIT 0')");
                }
            });

            Assert.Equal(runtimeAsync, rejected.Task.IsCompleted);
            Assert.Equal(runtimeAsync, pageError.Task.IsCompleted);
            Assert.Equal(!runtimeAsync, mainRan.Task.IsCompleted);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [TestCategory("mono")]
        public async Task RuntimeAsyncDependencyIsRejected(bool runtimeAsync)
        {
            Configuration config = Configuration.Debug;
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "runtime_async_dependency",
                extraItems: """
                    <ProjectReference Include="..\Library\Library.csproj" />
                    <EmbeddedResource Include="..\Library\bin\Debug\net11.0\Library.dll" LogicalName="Payload.dll" />
                    """);
            if (runtimeAsync)
            {
                UpdateFile(Path.Combine("..", "Library", "Library.csproj"), new Dictionary<string, string>
                {
                    { "</PropertyGroup>", "<Features>$(Features);runtime-async=on</Features></PropertyGroup>" }
                });
            }
            UpdateFile(Path.Combine("..", "Library", "Library.cs"), """
                using System.Threading.Tasks;

                public class AsyncDependency
                {
                    public static async Task<int> Unused() => await Task.FromResult(1);
                }
                """);
            UpdateFile(Path.Combine("Common", "Program.cs"), $$"""
                using System;
                using System.IO;
                using System.Reflection;
                using System.Runtime.InteropServices.JavaScript;

                public class TestClass
                {
                    public static int Main()
                    {
                        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.dll")!;
                        using var data = new MemoryStream();
                        stream.CopyTo(data);

                        bool bytesRejected = false;
                        #pragma warning disable IL2026
                        try { Assembly.Load(data.ToArray()); }
                        #pragma warning restore IL2026
                        catch (BadImageFormatException) { bytesRejected = true; }

                        bool nameRejected = false;
                        try { Assembly.Load("Library"); }
                        catch (FileNotFoundException) { nameRejected = true; }
                        catch (BadImageFormatException) { nameRejected = true; }

                        bool expectRejection = {{runtimeAsync.ToString().ToLowerInvariant()}};
                        bool exportRejected = false;
                        if (expectRejection)
                        {
                            try { JSFunctionBinding.BindManagedFunction("[Library]AsyncDependency:Unused", 0, default); }
                            catch (FileNotFoundException) { exportRejected = true; }
                        }

                        Console.WriteLine($"TestOutput -> bytes rejected: {bytesRejected}");
                        Console.WriteLine($"TestOutput -> name rejected: {nameRejected}");
                        Console.WriteLine($"TestOutput -> export rejected: {exportRejected}");
                        return bytesRejected == expectRejection &&
                               nameRejected == expectRejection &&
                               exportRejected == expectRejection ? 0 : 1;
                    }
                }
                """);
            BuildProject(info, config);
            AssertRuntimeAsyncFlag(Path.Combine(_projectDir, "..", "Library", "bin", "Debug", "net11.0", "Library.dll"), runtimeAsync);

            RunResult result = await RunForBuildWithDotnetRun(new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 0));
            Assert.Contains($"bytes rejected: {runtimeAsync}", result.TestOutput);
            Assert.Contains($"name rejected: {runtimeAsync}", result.TestOutput);
            Assert.Contains($"export rejected: {runtimeAsync}", result.TestOutput);
            if (runtimeAsync)
                Assert.Contains(result.ConsoleOutput, message => message.Contains("The assembly 'Library' contains runtime-async methods", StringComparison.Ordinal));
        }

        private static void AssertRuntimeAsyncFlag(string assemblyPath, bool expected)
        {
            using var stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);
            MetadataReader metadata = peReader.GetMetadataReader();
            MethodDefinitionHandle method = metadata.MethodDefinitions.Single(handle =>
                metadata.GetString(metadata.GetMethodDefinition(handle).Name) == "Unused");
            Assert.Equal(expected, (metadata.GetMethodDefinition(method).ImplAttributes & MethodImplAttributes.Async) != 0);
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
