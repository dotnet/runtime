// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests;

public class IcuTests : IcuTestsBase
{
    public IcuTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) { }

    public static IEnumerable<object[]> FullIcuWithICustomIcuTestData(Configuration config) =>
        from aot in boolOptions
            from fullIcu in boolOptions
            select new object[] { config, aot, fullIcu };

    public static IEnumerable<object[]> FullIcuWithInvariantTestData(Configuration config)
    {
        var locales = new object[][]
        {
            // in invariant mode, all locales should be missing
            new object[] { true, true, "Array.Empty<Locale>()" },
            new object[] { true, false, "Array.Empty<Locale>()" },
            new object[] { false, false, GetEfigsTestedLocales() },
            new object[] { false, true,  s_fullIcuTestedLocales }
        }; 
        return from aot in boolOptions
            from locale in locales
            select new object[] { config, aot, locale[0], locale[1], locale[2] };
    }

    public static IEnumerable<object[]> IncorrectIcuTestData(Configuration config)
    {
        var customFiles = new Dictionary<string, bool>
        {
            { "icudtNonExisting.dat", true },
            { "incorrectName.dat", false }
        };
        return from customFile in customFiles
            select new object[] { config, customFile.Key, customFile.Value };
    }
        

    [Theory]
    [MemberData(nameof(FullIcuWithInvariantTestData), parameters: new object[] { Configuration.Release })]
    public async Task FullIcuFromRuntimePackWithInvariant(Configuration config=Configuration.Release, bool aot=false, bool invariant=true, bool fullIcu=true, string testedLocales="Array.Empty<Locale>()") =>
        await PublishAndRunIcuTest(
            config,
            Template.WasmBrowser,
            aot,
            testedLocales,
            globalizationMode: invariant ? GlobalizationMode.Invariant : fullIcu ? GlobalizationMode.FullIcu : GlobalizationMode.Sharded,
            extraProperties:
                // https://github.com/dotnet/runtime/issues/94133: "wasmbrowser" should use WasmIncludeFullIcuData, not BlazorWebAssemblyLoadAllGlobalizationData
                $"<InvariantGlobalization>{invariant}</InvariantGlobalization><BlazorWebAssemblyLoadAllGlobalizationData>{fullIcu}</BlazorWebAssemblyLoadAllGlobalizationData><RunAOTCompilation>{aot}</RunAOTCompilation>");

    [Theory]
    [MemberData(nameof(FullIcuWithICustomIcuTestData), parameters: new object[] { Configuration.Release })]
    public async Task FullIcuFromRuntimePackWithCustomIcu(Configuration config, bool aot, bool fullIcu)
    {
        string customIcuProperty = "BlazorIcuDataFileName";
        string fullIcuProperty = "BlazorWebAssemblyLoadAllGlobalizationData";
        string extraProperties = $"<{customIcuProperty}>{CustomIcuPath}</{customIcuProperty}><{fullIcuProperty}>{fullIcu}</{fullIcuProperty}><RunAOTCompilation>{aot}</RunAOTCompilation>";
        
        string testedLocales = fullIcu ? s_fullIcuTestedLocales : s_customIcuTestedLocales;
        GlobalizationMode globalizationMode = fullIcu ? GlobalizationMode.FullIcu : GlobalizationMode.Custom;
        string customIcuFile = fullIcu ? "" : CustomIcuPath;
        string output = await PublishAndRunIcuTest(config, Template.WasmBrowser, aot, testedLocales, globalizationMode, extraProperties, icuFileName: customIcuFile);
        if (fullIcu)
            Assert.Contains($"$({customIcuProperty}) has no effect when $({fullIcuProperty}) is set to true.", output);
    }

    [Theory]
    [MemberData(nameof(IncorrectIcuTestData), parameters: new object[] { Configuration.Release })]
    public void NonExistingCustomFileAssertError(Configuration config, string customIcu, bool isFilenameFormCorrect)
    {        
        string customIcuProperty = "BlazorIcuDataFileName";
        string extraProperties = $"<{customIcuProperty}>{customIcu}</{customIcuProperty}>";
    
        ProjectInfo info = CreateIcuProject(config, Template.WasmBrowser, aot: false, "Array.Empty<Locale>()", extraProperties);
        (string _, string output) = BuildProject(info, config, new BuildOptions(
            GlobalizationMode: GlobalizationMode.Custom,
            CustomIcuFile: customIcu,
            ExpectSuccess: false,
            AssertAppBundle: false
        ));
        if (isFilenameFormCorrect)
        {
            Assert.Contains($"Could not find $({customIcuProperty})={customIcu}, or when used as a path relative to the runtime pack", output);
        }
        else
        {
            Assert.Contains($"File name in $({customIcuProperty}) has to start with 'icudt'.", output);
        }
    }
}
