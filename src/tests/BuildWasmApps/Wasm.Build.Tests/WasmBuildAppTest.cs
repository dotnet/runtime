// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmBuildAppTest : BuildTestBase
    {
        public WasmBuildAppTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : base(output, buildContext)
        {}

        public static IEnumerable<object?[]> MainMethodTestData(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void TopLevelMain(BuildArgs buildArgs, RunHost host, string id)
            => TestMain("top_level",
                    @"System.Console.WriteLine(""Hello, World!""); return await System.Threading.Tasks.Task.FromResult(42);",
                    buildArgs, host, id);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void AsyncMain(BuildArgs buildArgs, RunHost host, string id)
            => TestMain("async_main", @"
            using System;
            using System.Threading.Tasks;

            public class TestClass {
                public static async Task<int> Main()
                {
                    Console.WriteLine(""Hello, World!"");
                    return await Task.FromResult(42);
                }
            }", buildArgs, host, id);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void NonAsyncMain(BuildArgs buildArgs, RunHost host, string id)
            => TestMain("non_async_main", @"
                using System;
                using System.Threading.Tasks;

                public class TestClass {
                    public static int Main()
                    {
                        Console.WriteLine(""Hello, World!"");
                        return 42;
                    }
                }", buildArgs, host, id);

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
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void Bug49588_RegressionTest_AOT(BuildArgs buildArgs, RunHost host, string id)
            => TestMain("bug49588_aot", s_bug49588_ProgramCS, buildArgs, host, id);

        [Theory]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void Bug49588_RegressionTest_NativeRelinking(BuildArgs buildArgs, RunHost host, string id)
            => TestMain("bug49588_native_relinking", s_bug49588_ProgramCS, buildArgs, host, id,
                        extraProperties: "<WasmBuildNative>true</WasmBuildNative>",
                        dotnetWasmFromRuntimePack: false);

        void TestMain(string projectName,
                      string programText,
                      BuildArgs buildArgs,
                      RunHost host,
                      string id,
                      string extraProperties = "",
                      bool? dotnetWasmFromRuntimePack = null)
        {
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties);

            if (dotnetWasmFromRuntimePack == null)
                dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

            BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                        id: id,
                        dotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack);

            RunAndTestWasmApp(buildArgs, expectedExitCode: 42,
                                test: output => Assert.Contains("Hello, World!", output), host: host, id: id);
        }
    }

 }
