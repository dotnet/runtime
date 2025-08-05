// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class MainWithArgsTests : WasmTemplateTestsBase
    {
        public MainWithArgsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> MainWithArgsTestData(bool aot)
            => ConfigWithAOTData(aot).Multiply(
                        new object?[] { new object?[] { "abc", "foobar"} },
                        new object?[] { new object?[0] })
                .Where(item => !(item.ElementAt(0) is Configuration config && config == Configuration.Debug && item.ElementAt(1) is bool aotValue && aotValue))
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(MainWithArgsTestData), parameters: new object[] { /*aot*/ false })]
        [MemberData(nameof(MainWithArgsTestData), parameters: new object[] { /*aot*/ true })]
        public async Task AsyncMainWithArgs(Configuration config, bool aot, string[] args)
            => await TestMainWithArgs(config, aot, "async_main_with_args", "AsyncMainWithArgs.cs", args);

        [Theory]
        [MemberData(nameof(MainWithArgsTestData), parameters: new object[] { /*aot*/ false })]
        [MemberData(nameof(MainWithArgsTestData), parameters: new object[] { /*aot*/ true })]
        public async Task NonAsyncMainWithArgs(Configuration config, bool aot, string[] args)
            => await TestMainWithArgs(config, aot, "non_async_main_args", "SyncMainWithArgs.cs", args);

        async Task TestMainWithArgs(Configuration config,
                                bool aot,
                                string projectNamePrefix,
                                string projectContentsName,
                                string[] args)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, projectNamePrefix);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", projectContentsName));

            var queryArgs = new NameValueCollection();
            foreach (var arg in args)
                queryArgs.Add("arg", arg);
            PublishProject(info, config, new PublishOptions(AOT: aot));

            int argsCount = args.Length;
            int expectedCode = 42 + argsCount;
            RunResult output = await RunForPublishWithWebServer(
                new BrowserRunOptions(config, TestScenario: "MainWithArgs", BrowserQueryString: queryArgs, ExpectedExitCode: expectedCode));
            Assert.Contains(output.TestOutput, m => m.Contains($"args#: {argsCount}"));
            foreach (var arg in args)
                Assert.Contains(output.TestOutput, m => m.Contains($"arg: {arg}"));
        }
    }
}
