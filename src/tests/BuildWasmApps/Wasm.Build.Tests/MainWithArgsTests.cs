// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class MainWithArgsTests : BuildTestBase
    {
        public MainWithArgsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> MainWithArgsTestData(bool aot, RunHost host)
            => ConfigWithAOTData(aot).Multiply(
                        new object?[] { new object?[] { "abc", "foobar"} },
                        new object?[] { new object?[0] }
            ).WithRunHosts(host).UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(MainWithArgsTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        [MemberData(nameof(MainWithArgsTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void AsyncMainWithArgs(BuildArgs buildArgs, string[] args, RunHost host, string id)
            => TestMainWithArgs("async_main_with_args", @"
                public class TestClass {
                    public static async System.Threading.Tasks.Task<int> Main(string[] args)
                    {
                        ##CODE##
                        return await System.Threading.Tasks.Task.FromResult(42 + count);
                    }
                }",
                buildArgs, args, host, id);

        [Theory]
        [MemberData(nameof(MainWithArgsTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        [MemberData(nameof(MainWithArgsTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void TopLevelWithArgs(BuildArgs buildArgs, string[] args, RunHost host, string id)
            => TestMainWithArgs("top_level_args",
                                @"##CODE## return await System.Threading.Tasks.Task.FromResult(42 + count);",
                                buildArgs, args, host, id);

        [Theory]
        [MemberData(nameof(MainWithArgsTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        [MemberData(nameof(MainWithArgsTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void NonAsyncMainWithArgs(BuildArgs buildArgs, string[] args, RunHost host, string id)
            => TestMainWithArgs("non_async_main_args", @"
                public class TestClass {
                    public static int Main(string[] args)
                    {
                        ##CODE##
                        return 42 + count;
                    }
                }", buildArgs, args, host, id);

        void TestMainWithArgs(string projectNamePrefix,
                              string projectContents,
                              BuildArgs buildArgs,
                              string[] args,
                              RunHost host,
                              string id,
                              bool? dotnetWasmFromRuntimePack=null)
        {
            string projectName = $"{projectNamePrefix}_{buildArgs.Config}_{buildArgs.AOT}";
            string code = @"
                    int count = args == null ? 0 : args.Length;
                    System.Console.WriteLine($""args#: {args?.Length}"");
                    foreach (var arg in args ?? System.Array.Empty<string>())
                        System.Console.WriteLine($""arg: {arg}"");
                    ";
            string programText = projectContents.Replace("##CODE##", code);

            buildArgs = buildArgs with { ProjectName = projectName, ProjectFileContents = programText };
            buildArgs = ExpandBuildArgs(buildArgs);
            if (dotnetWasmFromRuntimePack == null)
                dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

            Console.WriteLine ($"-- args: {buildArgs}, name: {projectName}");

            BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                        id: id,
                        dotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack);

            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42 + args.Length, args: string.Join(' ', args),
                test: output =>
                {
                    Assert.Contains($"args#: {args.Length}", output);
                    foreach (var arg in args)
                        Assert.Contains($"arg: {arg}", output);
                }, host: host, id: id);
        }
    }
}
