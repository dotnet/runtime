// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests;

public class IcuShardingTests : IcuTestsBase
{
    public IcuShardingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) { }

    public static IEnumerable<object?[]> IcuExpectedAndMissingCustomShardTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { CustomIcuPath, s_customIcuTestedLocales, false },
                new object[] { CustomIcuPath, s_customIcuTestedLocales, true })
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    public static IEnumerable<object?[]> IcuExpectedAndMissingAutomaticShardTestData(bool aot)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { "fr-FR", GetEfigsTestedLocales(SundayNames.French)},
                new object[] { "ja-JP", GetCjkTestedLocales(SundayNames.Japanese) },
                new object[] { "sk-SK", GetNocjkTestedLocales(SundayNames.Slovak) })
            .WithRunHosts(BuildTestBase.s_hostsForOSLocaleSensitiveTests)
            .UnwrapItemsAsArrays();

    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingCustomShardTestData), parameters: new object[] { false, RunHost.NodeJS | RunHost.Chrome })]
    [MemberData(nameof(IcuExpectedAndMissingCustomShardTestData), parameters: new object[] { true, RunHost.NodeJS | RunHost.Chrome })]
    public void CustomIcuShard(BuildArgs buildArgs, string shardName, string testedLocales, bool onlyPredefinedCultures, RunHost host, string id) =>
        TestIcuShards(buildArgs, shardName, testedLocales, host, id, onlyPredefinedCultures);

    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingAutomaticShardTestData), parameters: new object[] { false })]
    [MemberData(nameof(IcuExpectedAndMissingAutomaticShardTestData), parameters: new object[] { true })]
    public void AutomaticShardSelectionDependingOnEnvLocale(BuildArgs buildArgs, string environmentLocale, string testedLocales, RunHost host, string id)
    {
        string projectName = $"automatic_shard_{environmentLocale}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        buildArgs = ExpandBuildArgs(buildArgs);

        string programText = GetProgramText(testedLocales);
        _testOutput.WriteLine($"----- Program: -----{Environment.NewLine}{programText}{Environment.NewLine}-------");
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack));
        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id, environmentLocale: environmentLocale);
    }
}
