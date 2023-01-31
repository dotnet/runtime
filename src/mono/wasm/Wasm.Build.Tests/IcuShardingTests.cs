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

public class IcuShardingTests : BuildTestBase
{
    public IcuShardingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) { }

    private static string customIcuPath = Path.Combine(BuildEnvironment.TestAssetsPath, "icudt_custom.dat");
    private static string[] customIcuExpectedLocales = new string[] { "cy-GB", "is-IS", "bs-BA", "lb-LU" };
    private static string[] customIcuMissingLocales = new string[] { "fr-FR", "hr-HR", "ko-KR" };

    public static IEnumerable<object?[]> IcuExpectedAndMissingAutomaticShardTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { "fr-FR", new string[] { "en-US", "fr-FR", "es-ES" }, new string[] { "pl-PL", "ko-KR", "cs-CZ" } },   // "icudt_EFIGS.dat"
                new object[] { "ja-JP", new string[] { "en-GB", "zh-CN", "ja-JP" }, new string[] { "fr-FR", "hr-HR", "it-IT" } },   // icudt_CJK.dat
                new object[] { "fr-CA", new string[] { "en-AU", "fr-FR", "sk-SK" }, new string[] { "ja-JP", "ko-KR", "zh-CN" } })   // "icudt_no_CJK.dat"
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    public static IEnumerable<object?[]> IcuExpectedAndMissingCustomShardTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                // custom file contains only 4 locales, nothing else:
                new object[] { customIcuPath, customIcuExpectedLocales, customIcuMissingLocales })
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    public static IEnumerable<object?[]> IcuExpectedAndMissingShardFromRuntimePackTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { "icudt.dat", new string[] { "en-GB", "zh-CN", "hr-HR" }, new string[] { "xx-yy" } },
                new object[] { "icudt_EFIGS.dat", new string[] { "en-US", "fr-FR", "es-ES" }, new string[] { "pl-PL", "ko-KR", "cs-CZ" } },
                new object[] { "icudt_CJK.dat", new string[] { "en-GB", "zh-CN", "ja-JP" }, new string[] { "fr-FR", "hr-HR", "it-IT" } },
                new object[] { "icudt_no_CJK.dat", new string[] { "en-AU", "fr-FR", "sk-SK" }, new string[] { "ja-JP", "ko-KR", "zh-CN"} })
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    public static IEnumerable<object?[]> FullIcuWithInvariantTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { true, true, new string[] { "en-GB", "pl-PL", "ko-KR" } },
                new object[] { true, false, new string[] { "en-GB", "pl-PL", "ko-KR" } },
                new object[] { false, false, new string[] { "en-GB", "fr-FR", "it-IT" } }, // default mode, only "icudt_EFIGS.dat" loaded
                new object[] { false, true, new string[] { "en-GB", "pl-PL", "ko-KR" } })
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    public static IEnumerable<object?[]> FullIcuWithICustomIcuTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { true },
                new object[] { false })
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    private static string GetLocalesStrArr(string[] locales) => locales.Length == 0 ? "Array.Empty<string>()" : $@"new string[] {{ ""{string.Join("\", \"", locales)}"" }}";

    private static string GetProgramText(string[] expectedLocales, string[] missingLocales) => $@"
        using System;
        using System.Globalization;

        string[] expectedLocales = {GetLocalesStrArr(expectedLocales)};
        string[] missingLocales =  {GetLocalesStrArr(missingLocales)};
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

    private void CheckExpectedLocales(string[] expectedLocales, string runOutput)
    {
        foreach (var loc in expectedLocales)
            Assert.Contains($"Found expected locale: {loc} - {loc}", runOutput);
    }

    private void CheckMissingLocales(string[] missingLocales, string runOutput)
    {
        foreach (var loc in missingLocales)
            Assert.Contains($"Missing locale as planned: {loc}", runOutput);
    }

    // [Theory]
    [MemberData(nameof(IcuExpectedAndMissingShardFromRuntimePackTestData), parameters: new object[] { false, RunHost.All })]
    [MemberData(nameof(IcuExpectedAndMissingShardFromRuntimePackTestData), parameters: new object[] { true, RunHost.All })]
    public void DefaultAvailableIcuShardsFromRuntimePack(BuildArgs buildArgs, string shardName, string[] expectedLocales, string[] missingLocales, RunHost host, string id) =>
        TestIcuShards(buildArgs, shardName, expectedLocales, missingLocales, host, id);

    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingCustomShardTestData), parameters: new object[] { false, RunHost.All })]
    [MemberData(nameof(IcuExpectedAndMissingCustomShardTestData), parameters: new object[] { true, RunHost.All })]
    public void CustomIcuShard(BuildArgs buildArgs, string shardName, string[] expectedLocales, string[] missingLocales, RunHost host, string id) =>
        TestIcuShards(buildArgs, shardName, expectedLocales, missingLocales, host, id);

    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingAutomaticShardTestData), parameters: new object[] { false, RunHost.All })]
    [MemberData(nameof(IcuExpectedAndMissingAutomaticShardTestData), parameters: new object[] { true, RunHost.All })]
    public void AutomaticShardSelectionDependingOnEnvLocale(BuildArgs buildArgs, string environmentLocale, string[] expectedLocales, string[] missingLocales, RunHost host, string id)
    {
        string projectName = $"automatic_shard_{environmentLocale}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        buildArgs = ExpandBuildArgs(buildArgs);

        string programText = GetProgramText(expectedLocales, missingLocales);
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack));

        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id, environmentLocale: environmentLocale);

        CheckExpectedLocales(expectedLocales, runOutput);
        CheckMissingLocales(missingLocales, runOutput);
    }

    // on Chrome: when loading only EFIGS or only CJK, CoreLib's failure on culture not found cannot be easily caught:
    // Encountered infinite recursion while looking up resource 'Argument_CultureNotSupported' in System.Private.CoreLib.
    private void TestIcuShards(BuildArgs buildArgs, string shardName, string[] expectedLocales, string[] missingLocales, RunHost host, string id)
    {
        string projectName = $"shard_{Path.GetFileName(shardName)}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        buildArgs = ExpandBuildArgs(buildArgs, extraProperties: $"<WasmIcuDataFileName>{shardName}</WasmIcuDataFileName>");

        string programText = GetProgramText(expectedLocales, missingLocales);
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                            GlobalizationMode: GlobalizationMode.PredefinedIcu,
                            PredefinedIcudt: shardName));

        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);

        CheckExpectedLocales(expectedLocales, runOutput);
        CheckMissingLocales(missingLocales, runOutput);
    }

    [Theory]
    [MemberData(nameof(FullIcuWithInvariantTestData), parameters: new object[] { false, RunHost.All })]
    [MemberData(nameof(FullIcuWithInvariantTestData), parameters: new object[] { true, RunHost.All })]
    public void FullIcuFromRuntimePackWithInvariant(BuildArgs buildArgs, bool invariant, bool fullIcu, string[] testedLocales, RunHost host, string id)
    {
        string projectName = $"fullIcuInvariant_{fullIcu}_{invariant}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        buildArgs = ExpandBuildArgs(buildArgs, extraProperties: $"<InvariantGlobalization>{invariant}</InvariantGlobalization><WasmIncludeFullIcuData>{fullIcu}</WasmIncludeFullIcuData>");

        // in invariant mode, all locales should be missing
        string[] expectedLocales = invariant ? Array.Empty<string>() : testedLocales;
        string[] missingLocales = invariant ? testedLocales : Array.Empty<string>();
        string programText = GetProgramText(expectedLocales, missingLocales);
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                            GlobalizationMode: invariant ? GlobalizationMode.Invariant : fullIcu ? GlobalizationMode.FullIcu : null));

        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);

        CheckExpectedLocales(expectedLocales, runOutput);
        CheckMissingLocales(missingLocales, runOutput);
    }

    [Theory]
    [MemberData(nameof(FullIcuWithICustomIcuTestData), parameters: new object[] { false, RunHost.All })]
    [MemberData(nameof(FullIcuWithICustomIcuTestData), parameters: new object[] { true, RunHost.All })]
    public void FullIcuFromRuntimePackWithCustomIcu(BuildArgs buildArgs, bool hasCustomIcu, RunHost host, string id)
    {
        string projectName = $"fullIcuCustom_{hasCustomIcu}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        if (hasCustomIcu)
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties: $"<WasmIcuDataFileName>{customIcuPath}</WasmIcuDataFileName>");

        // custom icu has locales that are not present in full icu data and the other way around
        string[] expectedLocales = hasCustomIcu ? customIcuExpectedLocales : customIcuMissingLocales;
        string[] missingLocales = hasCustomIcu ? customIcuMissingLocales : customIcuExpectedLocales;
        string programText = GetProgramText(expectedLocales, missingLocales);
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                            GlobalizationMode: hasCustomIcu ? GlobalizationMode.PredefinedIcu : GlobalizationMode.FullIcu,
                            PredefinedIcudt: hasCustomIcu ? customIcuPath : ""));

        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);

        if (hasCustomIcu)
            Assert.Contains("$(WasmIcuDataFileName) has no effect when $(WasmIncludeFullIcuData) is set to true.", runOutput);
        CheckExpectedLocales(expectedLocales, runOutput);
        CheckMissingLocales(missingLocales, runOutput);
    }

    [Theory]
    [BuildAndRun(host: RunHost.None)]
    public void NonExistingCustomFileAssertError(BuildArgs buildArgs, string id)
    {
        string projectName = $"invalidCustomIcu_{buildArgs.Config}_{buildArgs.AOT}";
        buildArgs = buildArgs with { ProjectName = projectName };
        buildArgs = ExpandBuildArgs(buildArgs, extraProperties: $"<WasmIcuDataFileName>nonexisting.dat</WasmIcuDataFileName>");

        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                            ExpectSuccess: false));
        Assert.Contains("File in location $(WasmIcuDataFileName)=nonexisting.dat cannot be found neither when used as absolute path nor a relative runtime pack path.", output);
    }
}
