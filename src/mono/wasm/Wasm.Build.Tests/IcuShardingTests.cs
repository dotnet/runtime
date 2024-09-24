// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public class IcuShardingTests : IcuTestsBase
{
    public IcuShardingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) { }

    public static IEnumerable<object[]> IcuExpectedAndMissingCustomShardTestData(string config)
    {
        string[] templateTypes = { "wasmbrowser" }; // { "wasmconsole", "wasmbrowser" };
        bool[] isAot = { false, true };
        bool[] isOnlyPredefinedCultures = { false, true };

        return from templateType in templateTypes
                           from aot in isAot
                           from onlyPredefinedCultures in isOnlyPredefinedCultures
                           select new object[] { config, templateType, aot, CustomIcuPath, s_customIcuTestedLocales, onlyPredefinedCultures };
    }

    public static IEnumerable<object?[]> IcuExpectedAndMissingAutomaticShardTestData(bool aot)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { "fr-FR", GetEfigsTestedLocales(SundayNames.French)},
                new object[] { "ja-JP", GetCjkTestedLocales(SundayNames.Japanese) },
                new object[] { "sk-SK", GetNocjkTestedLocales(SundayNames.Slovak) })
            .WithRunHosts(BuildTestBase.s_hostsForOSLocaleSensitiveTests)
            .UnwrapItemsAsArrays();

    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingCustomShardTestData), parameters: new object[] { "Release" })]
    public async Task CustomIcuShard(string config, string templateType, bool aot, string customIcuPath, string customLocales, bool onlyPredefinedCultures) =>
        await TestIcuShards(config, templateType, aot, customIcuPath, customLocales, GlobalizationMode.Custom, onlyPredefinedCultures);

    // [Theory]
    // [MemberData(nameof(IcuExpectedAndMissingAutomaticShardTestData), parameters: new object[] { false })]
    // [MemberData(nameof(IcuExpectedAndMissingAutomaticShardTestData), parameters: new object[] { true })]
    // public void AutomaticShardSelectionDependingOnEnvLocale(BuildArgs buildArgs, string environmentLocale, string testedLocales, RunHost host, string id)
    // {
    //     string projectName = $"automatic_shard_{environmentLocale}_{buildArgs.Config}_{buildArgs.AOT}";
    //     bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

    //     buildArgs = buildArgs with { ProjectName = projectName };
    //     buildArgs = ExpandBuildArgs(buildArgs);

    //     string programText = GetProgramText(testedLocales);
    //     _testOutput.WriteLine($"----- Program: -----{Environment.NewLine}{programText}{Environment.NewLine}-------");
    //     (_, string output) = BuildProject(buildArgs,
    //                     id: id,
    //                     new BuildProjectOptions(
    //                         InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
    //                         DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack));
    //     string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id, environmentLocale: environmentLocale);
    // }
}
