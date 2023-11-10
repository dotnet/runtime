// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public abstract class IcuTestsBase : TestMainJsTestBase
{
    public IcuTestsBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) { }

    private const string _fallbackSundayNameEnUS = "Sunday";

    protected record SundayNames
    {
        public static string English = "Sunday";
        public static string French = "dimanche";
        public static string Spanish = "domingo";
        public static string Chinese = "星期日";
        public static string Japanese = "日曜日";
        public static string Slovak = "nedeľa";
    }

    // custom file contains only locales "cy-GB", "is-IS", "bs-BA", "lb-LU" and fallback locale: "en-US":
    public static string CustomIcuPath = Path.Combine(BuildEnvironment.TestAssetsPath, "icudt_custom.dat");

    protected static readonly string s_customIcuTestedLocales = $@"new Locale[] {{
        new Locale(""cy-GB"",  ""Dydd Sul""), new Locale(""is-IS"",  ""sunnudagur""), new Locale(""bs-BA"",  ""nedjelja""), new Locale(""lb-LU"",  ""Sonndeg""),
        new Locale(""fr-FR"", null), new Locale(""hr-HR"", null), new Locale(""ko-KR"", null)
    }}";
    protected static string GetEfigsTestedLocales(string fallbackSundayName=_fallbackSundayNameEnUS) =>  $@"new Locale[] {{
        new Locale(""en-US"", ""{SundayNames.English}""), new Locale(""fr-FR"", ""{SundayNames.French}""), new Locale(""es-ES"", ""{SundayNames.Spanish}""),
        new Locale(""pl-PL"", ""{fallbackSundayName}""), new Locale(""ko-KR"", ""{fallbackSundayName}""), new Locale(""cs-CZ"", ""{fallbackSundayName}"")
    }}";
    protected static string GetCjkTestedLocales(string fallbackSundayName=_fallbackSundayNameEnUS) =>  $@"new Locale[] {{
        new Locale(""en-GB"", ""{SundayNames.English}""), new Locale(""zh-CN"", ""{SundayNames.Chinese}""), new Locale(""ja-JP"", ""{SundayNames.Japanese}""),
        new Locale(""fr-FR"", ""{fallbackSundayName}""), new Locale(""hr-HR"", ""{fallbackSundayName}""), new Locale(""it-IT"", ""{fallbackSundayName}"")
    }}";
    protected static string GetNocjkTestedLocales(string fallbackSundayName=_fallbackSundayNameEnUS) =>  $@"new Locale[] {{
        new Locale(""en-AU"", ""{SundayNames.English}""), new Locale(""fr-FR"", ""{SundayNames.French}""), new Locale(""sk-SK"", ""{SundayNames.Slovak}""),
        new Locale(""ja-JP"", ""{fallbackSundayName}""), new Locale(""ko-KR"", ""{fallbackSundayName}""), new Locale(""zh-CN"", ""{fallbackSundayName}"")
    }}";
    protected static readonly string s_fullIcuTestedLocales = $@"new Locale[] {{
        new Locale(""en-GB"", ""{SundayNames.English}""), new Locale(""sk-SK"", ""{SundayNames.Slovak}""), new Locale(""zh-CN"", ""{SundayNames.Chinese}"")
    }}";

    protected string GetProgramText(string testedLocales, bool onlyPredefinedCultures=false, string fallbackSundayName=_fallbackSundayNameEnUS) => $@"
        #nullable enable

        using System;
        using System.Globalization;

        Console.WriteLine($""Current culture: '{{CultureInfo.CurrentCulture.Name}}'"");

        string fallbackSundayName = ""{fallbackSundayName}"";
        bool onlyPredefinedCultures = {(onlyPredefinedCultures ? "true" : "false")};
        Locale[] localesToTest = {testedLocales};

        bool fail = false;
        foreach (var testLocale in localesToTest)
        {{
            bool expectMissing = string.IsNullOrEmpty(testLocale.SundayName);
            bool ctorShouldFail = expectMissing && onlyPredefinedCultures;
            CultureInfo culture;

            try
            {{
                culture = new CultureInfo(testLocale.Code);
                if (ctorShouldFail)
                {{
                    Console.WriteLine($""CultureInfo..ctor did not throw an exception for {{testLocale.Code}} as was expected."");
                    fail = true;
                    continue;
                }}
            }}
            catch(CultureNotFoundException cnfe) when (ctorShouldFail && cnfe.Message.Contains($""{{testLocale.Code}} is an invalid culture identifier.""))
            {{
                Console.WriteLine($""{{testLocale.Code}}: Success. .ctor failed as expected."");
                continue;
            }}

            string expectedSundayName = (expectMissing && !onlyPredefinedCultures)
                                            ? fallbackSundayName
                                            : testLocale.SundayName;
            var actualLocalizedSundayName = culture.DateTimeFormat.GetDayName(new DateTime(2000,01,02).DayOfWeek);
            if (expectedSundayName != actualLocalizedSundayName)
            {{
                Console.WriteLine($""Error: incorrect localized value for Sunday in locale {{testLocale.Code}}. Expected '{{expectedSundayName}}' but got '{{actualLocalizedSundayName}}'."");
                fail = true;
                continue;
            }}
            Console.WriteLine($""{{testLocale.Code}}: Success. Sunday name: {{actualLocalizedSundayName}}"");
        }}
        return fail ? -1 : 42;

        public record Locale(string Code, string? SundayName);
        ";

    protected void TestIcuShards(BuildArgs buildArgs, string shardName, string testedLocales, RunHost host, string id, bool onlyPredefinedCultures=false)
    {
        string projectName = $"shard_{Path.GetFileName(shardName)}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        // by default, we remove resource strings from an app. ICU tests are checking exception messages contents -> resource string keys are not enough
        string extraProperties = $"<WasmIcuDataFileName>{shardName}</WasmIcuDataFileName><UseSystemResourceKeys>false</UseSystemResourceKeys>";
        if (onlyPredefinedCultures)
            extraProperties = $"{extraProperties}<PredefinedCulturesOnly>true</PredefinedCulturesOnly>";
        buildArgs = ExpandBuildArgs(buildArgs, extraProperties: extraProperties);

        string programText = GetProgramText(testedLocales, onlyPredefinedCultures);
        _testOutput.WriteLine($"----- Program: -----{Environment.NewLine}{programText}{Environment.NewLine}-------");
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                            GlobalizationMode: GlobalizationMode.PredefinedIcu,
                            PredefinedIcudt: shardName));

        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
    }
}
