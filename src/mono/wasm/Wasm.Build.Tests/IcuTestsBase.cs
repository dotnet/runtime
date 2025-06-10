// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public abstract class IcuTestsBase : WasmTemplateTestsBase
{
    public IcuTestsBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) { }

    private const string _fallbackSundayNameEnUS = "Sunday";
    protected static string[] templateTypes = { "wasmbrowser" };
    protected static bool[] boolOptions =  { false, true };

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
            catch (CultureNotFoundException cnfe) when (ctorShouldFail && cnfe.Message.Contains($""{{testLocale.Code}} is an invalid culture identifier.""))
            {{
                Console.WriteLine($""{{testLocale.Code}}: Success. .ctor failed as expected."");
                continue;
            }}

            string expectedSundayName = (expectMissing && !onlyPredefinedCultures)
                                            ? fallbackSundayName
                                            : testLocale.SundayName ?? fallbackSundayName;
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

    protected async Task TestIcuShards(Configuration config, Template templateType, bool aot, string shardName, string testedLocales, GlobalizationMode globalizationMode, bool onlyPredefinedCultures=false)
    {
        string icuProperty = "BlazorIcuDataFileName"; // https://github.com/dotnet/runtime/issues/94133
        // by default, we remove resource strings from an app. ICU tests are checking exception messages contents -> resource string keys are not enough
        string extraProperties = $"<{icuProperty}>{shardName}</{icuProperty}><UseSystemResourceKeys>false</UseSystemResourceKeys><RunAOTCompilation>{aot}</RunAOTCompilation>";
        if (onlyPredefinedCultures)
            extraProperties = $"{extraProperties}<PredefinedCulturesOnly>true</PredefinedCulturesOnly>";
        await PublishAndRunIcuTest(config, templateType, aot, testedLocales, globalizationMode, extraProperties, onlyPredefinedCultures, icuFileName: shardName);
    }

    protected ProjectInfo CreateIcuProject(
        Configuration config,
        Template templateType,
        bool aot,
        string testedLocales,
        string extraProperties = "",
        bool onlyPredefinedCultures=false)
    {
        ProjectInfo info = CreateWasmTemplateProject(templateType, config, aot, "icu", extraProperties: extraProperties);
        string projectDirectory = Path.GetDirectoryName(info.ProjectFilePath)!;
        string programPath = Path.Combine(projectDirectory, "Program.cs");
        string programText = GetProgramText(testedLocales, onlyPredefinedCultures);
        File.WriteAllText(programPath, programText);
        _testOutput.WriteLine($"----- Program: -----{Environment.NewLine}{programText}{Environment.NewLine}-------");

        UpdateBrowserMainJs();
        return info;
    }

    protected async Task<string> PublishAndRunIcuTest(
        Configuration config,
        Template templateType,
        bool aot,
        string testedLocales,
        GlobalizationMode globalizationMode,
        string extraProperties = "",
        bool onlyPredefinedCultures=false,
        string locale = "en-US",
        string icuFileName = "")
    {
        try
        {
            ProjectInfo info = CreateIcuProject(
                config, templateType, aot, testedLocales, extraProperties, onlyPredefinedCultures);
            bool triggersNativeBuild = globalizationMode == GlobalizationMode.Invariant;
            (string _, string buildOutput) = PublishProject(info,
                config,
                new PublishOptions(GlobalizationMode: globalizationMode, CustomIcuFile: icuFileName),
                isNativeBuild: triggersNativeBuild ? true : null);

            BrowserRunOptions runOptions = new(config, Locale: locale, ExpectedExitCode: 42);
            RunResult runOutput = await RunForPublishWithWebServer(runOptions);
            return $"{buildOutput}\n{runOutput.TestOutput}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex}; _testOutput={_testOutput}");
            throw;
        }
    }
}
