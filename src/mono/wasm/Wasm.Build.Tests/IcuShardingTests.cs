// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests;

public class IcuShardingTests : BuildTestBase
{
    public IcuShardingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    public static IEnumerable<object?[]> IcuExpectedAndMissingShardTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { "icudt.dat", new string[] { "en-GB", "zh-CN", "hr-HR" }, new string[] { "xx-yy" } },
                new object[] { "icudt_EFIGS.dat", new string[] { "en-US", "fr-FR", "es-ES" }, new string[] { "pl-PL", "ko-KR", "cs-CZ" } },
                new object[] { "icudt_CJK.dat", new string[] { "en-GB", "zh-CN", "ja-JP" }, new string[] { "fr-FR", "hr-HR", "it-IT" } },
                new object[] { "icudt_no_CJK.dat", new string[] { "en-AU", "fr-FR", "sk-SK" }, new string[] { "ja-JP", "ko-KR", "zh-CN"} })
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    private static string GetProgramText(string[] expectedLocales, string[] missingLocales) => $@"
        using System;
        using System.Globalization;

        string[] expectedLocales = new string[] {{ "" {string.Join("\", \"", expectedLocales)} "" }};
        string[] missingLocales =  new string[] {{ "" {string.Join("\", \"", missingLocales)} "" }};
        foreach (var loc in expectedLocales)
        {{
            var culture = new CultureInfo(loc);
            Console.WriteLine($""Found expected locale: {{loc}} - {{culture.Name}}"");
        }}
        foreach (var loc in missingLocales)
        {{
            try
            {{
                var culture = new CultureInfo(loc);
            }}
            catch(Exception)
            {{
            Console.WriteLine($""Missing locale as planned: {{loc}}"");
            }}
        }}
        return 42;
        ";

    // on Chrome: when loading only EFIGS or only CJK, CoreLib's failure on culture not found cannot be easily caught:
    // Encountered infinite recursion while looking up resource 'Argument_CultureNotSupported' in System.Private.CoreLib.
    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingShardTestData), parameters: new object[] { false, RunHost.All })]
    [MemberData(nameof(IcuExpectedAndMissingShardTestData), parameters: new object[] { true, RunHost.All })]
    public void TestIcuShard(BuildArgs buildArgs, string shardName, string[] expectedLocales, string[] missingLocales, RunHost host, string id)
    {
        string projectName = $"shard_{shardName}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        buildArgs = ExpandBuildArgs(buildArgs, extraProperties: $"<IcuFileName>{shardName}</IcuFileName>");

        string programText = GetProgramText(expectedLocales, missingLocales);
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                            PredefinedIcudt: shardName));

        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42,
                    test: output => {},
                    host: host, id: id);

        foreach (var loc in expectedLocales)
            Assert.Contains($"Found expected locale: {loc} - {loc}", runOutput);

        foreach (var loc in missingLocales)
            Assert.Contains($"Missing locale as planned: {loc}", runOutput);
    }
}
