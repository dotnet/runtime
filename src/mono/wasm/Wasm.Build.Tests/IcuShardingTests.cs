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

    // custom file contains only locales "cy-GB", "is-IS", "bs-BA", "lb-LU" and fallback locale: "en-US":
    private static string customIcuPath = Path.Combine(BuildEnvironment.TestAssetsPath, "icudt_custom.dat");
    public record SundayNames {
        public static string French = "dimanche";
        public static string Spanish = "domingo";
        public static string Chinese = "星期日";
        public static string Japanese = "日曜日";
        public static string Slovak = "nedeľa";
    }
    private static string customIcuTestedLocales = $@"new Locale[] {{
        new Locale(""cy-GB"",  ""Dydd Sul""), new Locale(""is-IS"",  ""sunnudagur""), new Locale(""bs-BA"",  ""nedjelja""), new Locale(""lb-LU"",  ""Sonndeg""),
        new Locale(""fr-FR""), new Locale(""hr-HR""), new Locale(""ko-KR"")
    }}";
    private static string GetEfigsTestedLocales(string fallbackSundayName="Sunday") =>  $@"new Locale[] {{
        new Locale(""en-US""), new Locale(""fr-FR"", ""{SundayNames.French}""), new Locale(""es-ES"", ""{SundayNames.Spanish}""),
        new Locale(""pl-PL"", ""{fallbackSundayName}""), new Locale(""ko-KR"", ""{fallbackSundayName}""), new Locale(""cs-CZ"", ""{fallbackSundayName}"")
    }}";
    private static string GetCjkTestedLocales(string fallbackSundayName="Sunday") =>  $@"new Locale[] {{
        new Locale(""en-GB""), new Locale(""zh-CN"", ""{SundayNames.Chinese}""), new Locale(""ja-JP"", ""{SundayNames.Japanese}""),
        new Locale(""fr-FR"", ""{fallbackSundayName}""), new Locale(""hr-HR"", ""{fallbackSundayName}""), new Locale(""it-IT"", ""{fallbackSundayName}"")
    }}";
    private static string GetNocjkTestedLocales(string fallbackSundayName="Sunday") =>  $@"new Locale[] {{
        new Locale(""en-AU""), new Locale(""fr-FR"", ""{SundayNames.French}""), new Locale(""sk-SK"", ""{SundayNames.Slovak}""),
        new Locale(""ja-JP"", ""{fallbackSundayName}""), new Locale(""ko-KR"", ""{fallbackSundayName}""), new Locale(""zh-CN"", ""{fallbackSundayName}"")
    }}";
    private static string fullIcuTestedLocales = $@"new Locale[] {{
        new Locale(""en-GB""), new Locale(""sk-SK"", ""{SundayNames.Slovak}""), new Locale(""zh-CN"", ""{SundayNames.Chinese}"")
    }}";

    public static IEnumerable<object?[]> IcuExpectedAndMissingCustomShardTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { customIcuPath, customIcuTestedLocales, false },
                new object[] { customIcuPath, customIcuTestedLocales, true })
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    public static IEnumerable<object?[]> IcuExpectedAndMissingShardFromRuntimePackTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { "icudt.dat",
                                $@"new Locale[] {{
                                    new Locale(""en-GB""), new Locale(""zh-CN"", ""{SundayNames.Chinese}""), new Locale(""sk-SK"", ""{SundayNames.Slovak}""),
                                    new Locale(""xx-yy"") }}" },
                new object[] { "icudt_EFIGS.dat", GetEfigsTestedLocales() },
                new object[] { "icudt_CJK.dat", GetCjkTestedLocales() },
                new object[] { "icudt_no_CJK.dat", GetNocjkTestedLocales() })
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    public static IEnumerable<object?[]> IcuExpectedAndMissingAutomaticShardTestData(bool aot)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { "fr-FR", GetEfigsTestedLocales(SundayNames.French)},
                new object[] { "ja-JP", GetCjkTestedLocales(SundayNames.Japanese) },
                new object[] { "sk-SK", GetNocjkTestedLocales(SundayNames.Slovak) })
            .WithRunHosts(BuildTestBase.hostsForOSLocaleSensitiveTests)
            .UnwrapItemsAsArrays();

    public static IEnumerable<object?[]> FullIcuWithInvariantTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                // in invariant mode, all locales should be missing
                new object[] { true, true, "Array.Empty<Locale>()" },
                new object[] { true, false, "Array.Empty<Locale>()" },
                new object[] { false, false, GetEfigsTestedLocales() },
                new object[] { false, true,  fullIcuTestedLocales})
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    public static IEnumerable<object?[]> FullIcuWithICustomIcuTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { true },
                new object[] { false })
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    private static string GetProgramText(string testedLocales, bool onlyPredefinedCultures=false) => $@"
        using System;
        using System.Globalization;

        bool onlyPredefinedCultures = {onlyPredefinedCultures.ToString().ToLower()};
        Locale[] locales = {testedLocales};
        foreach (var loc in locales)
        {{
            try
            {{
                var culture = new CultureInfo(loc.Code);
                var localizedSundayName = culture.DateTimeFormat.GetDayName(new DateTime(2000,01,02).DayOfWeek);
                if (loc.SundayName != localizedSundayName)
                {{
                    Console.WriteLine($""Error: incorrect localized value for Sunday in locale {{loc.Code}}. Expected {{loc.SundayName}} but got {{localizedSundayName}}."");
                    return -1;
                }}
            }}
            catch(Exception ex)
            {{
                if (!onlyPredefinedCultures)
                {{
                    Console.WriteLine($""Error: for locale {{loc.Code}} encountered an exception. This is expected only when OnlyPredefinedCultures=true. Ex: {{ex}}."");
                    return -1;
                }}
                if (!ex.Message.Contains(""Culture is not supported""))
                {{
                    Console.WriteLine($""Error: for locale {{loc.Code}} encountered an exception from different reason than expected. Ex: {{ex}}."");
                    return -1;
                }}
            }}
        }}
        return 42;

        public record Locale(string Code, string SundayName=""Sunday"");
        ";

    private void TestIcuShards(BuildArgs buildArgs, string shardName, string testedLocales, RunHost host, string id, bool onlyPredefinedCultures=false)
    {
        string projectName = $"shard_{Path.GetFileName(shardName)}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        string extraProperties = onlyPredefinedCultures ?
            $"<WasmIcuDataFileName>{shardName}</WasmIcuDataFileName><PredefinedCulturesOnly>true</PredefinedCulturesOnly>" :
            $"<WasmIcuDataFileName>{shardName}</WasmIcuDataFileName>";
        buildArgs = ExpandBuildArgs(buildArgs, extraProperties: extraProperties);

        string programText = GetProgramText(testedLocales, onlyPredefinedCultures);
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                            GlobalizationMode: GlobalizationMode.PredefinedIcu,
                            PredefinedIcudt: shardName));

        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
    }

    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingCustomShardTestData), parameters: new object[] { false, RunHost.NodeJS | RunHost.Chrome })]
    [MemberData(nameof(IcuExpectedAndMissingCustomShardTestData), parameters: new object[] { true, RunHost.NodeJS | RunHost.Chrome })]
    public void CustomIcuShard(BuildArgs buildArgs, string shardName, string testedLocales, bool onlyPredefinedCultures, RunHost host, string id) =>
        TestIcuShards(buildArgs, shardName, testedLocales, host, id, onlyPredefinedCultures);

    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingShardFromRuntimePackTestData), parameters: new object[] { false,RunHost.NodeJS | RunHost.Chrome })]
    [MemberData(nameof(IcuExpectedAndMissingShardFromRuntimePackTestData), parameters: new object[] { true, RunHost.NodeJS | RunHost.Chrome })]
    public void DefaultAvailableIcuShardsFromRuntimePack(BuildArgs buildArgs, string shardName, string testedLocales, RunHost host, string id) =>
        TestIcuShards(buildArgs, shardName, testedLocales, host, id);

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
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack));
        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id, environmentLocale: environmentLocale);
    }

    [Theory]
    [MemberData(nameof(FullIcuWithInvariantTestData), parameters: new object[] { false, RunHost.NodeJS | RunHost.Chrome })]
    [MemberData(nameof(FullIcuWithInvariantTestData), parameters: new object[] { true, RunHost.NodeJS | RunHost.Chrome })]
    public void FullIcuFromRuntimePackWithInvariant(BuildArgs buildArgs, bool invariant, bool fullIcu, string testedLocales, RunHost host, string id)
    {
        string projectName = $"fullIcuInvariant_{fullIcu}_{invariant}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        buildArgs = ExpandBuildArgs(buildArgs, extraProperties: $"<InvariantGlobalization>{invariant}</InvariantGlobalization><WasmIncludeFullIcuData>{fullIcu}</WasmIncludeFullIcuData>");

        string programText = GetProgramText(testedLocales);
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                            GlobalizationMode: invariant ? GlobalizationMode.Invariant : fullIcu ? GlobalizationMode.FullIcu : null));

        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
    }

    [Theory]
    [MemberData(nameof(FullIcuWithICustomIcuTestData), parameters: new object[] { false, RunHost.NodeJS | RunHost.Chrome })]
    [MemberData(nameof(FullIcuWithICustomIcuTestData), parameters: new object[] { true, RunHost.NodeJS | RunHost.Chrome })]
    public void FullIcuFromRuntimePackWithCustomIcu(BuildArgs buildArgs, bool fullIcu, RunHost host, string id)
    {
        string projectName = $"fullIcuCustom_{fullIcu}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        buildArgs = ExpandBuildArgs(buildArgs, extraProperties: $"<WasmIcuDataFileName>{customIcuPath}</WasmIcuDataFileName><WasmIncludeFullIcuData>{fullIcu}</WasmIncludeFullIcuData>");

        string testedLocales = fullIcu ? fullIcuTestedLocales : customIcuTestedLocales;
        string programText = GetProgramText(testedLocales);
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                            GlobalizationMode: fullIcu ? GlobalizationMode.FullIcu : GlobalizationMode.PredefinedIcu,
                            PredefinedIcudt: fullIcu ? "" : customIcuPath));
        if (fullIcu)
            Assert.Contains("$(WasmIcuDataFileName) has no effect when $(WasmIncludeFullIcuData) is set to true.", output);

        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
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
