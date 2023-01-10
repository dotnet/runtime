// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests
{
    public class IcuShardingTests : BuildTestBase
    {
        public IcuShardingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

         public static IEnumerable<object?[]> BuildAndRun_ShardData(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .Multiply(ShardData)
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        public static IEnumerable<object[]> ShardData()
        {
            yield return new object[] { "icudt_EFIGS.dat", "new string[] { \"en-US\", \"fr-FR\" }", "new string[] { \"pl-PL\", \"ko-KR\" \"cs-CZ\" }" };
            yield return new object[] { "icudt_CJK.dat", "new string[] { \"en-GB\", \"zh-CN\" }", "new string[] { \"fr-FR\", \"hr-HR\", \"it-IT\" }" };
            yield return new object[] { "icudt_no_CJK.dat", "new string[] { \"en-AU\", \"fr-FR\", \"sk-SK\" }", "new string[] { \"ja-JP\", \"ko-KR\", \"zh-CN\"}" };
        }

        protected static string GetProgramText(string expectedLocales, string missingLocales) => $@"
            using System;
            using System.Globalization;

            string[] expectedLocales = {expectedLocales};
            string[] missingLocales = {missingLocales};
            foreach (var loc in expectedLocales)
            {{
                var culture = new CultureInfo(loc);
            }}
            foreach (var loc in missingLocales)
            {{
                try
                {{
                    var culture = new CultureInfo(loc);
                }}
                catch()
                {{
                    Console.WriteLine(""failed"");
                }}
            }}
            return 42;
            ";

        [Theory]
        [MemberData(nameof(BuildAndRun_ShardData), parameters: new object[] { false, RunHost.All })]
        [MemberData(nameof(BuildAndRun_ShardData), parameters: new object[] { true, RunHost.All })]
        public void TestIcuShard(BuildArgs buildArgs, string shardName, string expectedLocales, string missingLocales, RunHost host, string id)
        {
            string projectName = $"shard_{shardName}_{buildArgs.Config}_{buildArgs.AOT}";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties: $"<IcuFileName>{shardName}</IcuFileName>");

            string programTest = GetProgramText(expectedLocales, missingLocales);
            (_, string output) = BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programTest),
                                DotnetWasmFromRuntimePack: true,
                                CustomIcudt: shardName));
            System.Console.WriteLine($"Build output: {output}");

            string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42,
                        test: output => {},
                        host: host, id: id);
            Assert.Equals("failed\nfailed\nfailed\n", runOutput);
            System.Console.WriteLine($"Run output: {runOutput}");
        }
    }
}
