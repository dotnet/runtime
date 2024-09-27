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

    public static IEnumerable<object[]> FullIcuWithICustomIcuTestData(string config) =>
        from templateType in templateTypes
            from aot in boolOptions
            from fullIcu in boolOptions
            select new object[] { config, templateType, aot, fullIcu };

    public static IEnumerable<object[]> FullIcuWithInvariantTestData(string config)
    {
        var locales = new object[][]
        {
            // in invariant mode, all locales should be missing
            new object[] { true, true, "Array.Empty<Locale>()" },
            new object[] { true, false, "Array.Empty<Locale>()" },
            new object[] { false, false, GetEfigsTestedLocales() },
            new object[] { false, true,  s_fullIcuTestedLocales }
        }; 
        return from templateType in templateTypes
            from aot in boolOptions
            from locale in locales
            select new object[] { config, templateType, aot, locale[0], locale[1], locale[2] };
    }

    public static IEnumerable<object[]> IncorrectIcuTestData(string config)
    {
        var customFiles = new Dictionary<string, bool>
        {
            { "icudtNonExisting.dat", true },
            { "incorrectName.dat", false }
        };
        return from templateType in templateTypes
            from customFile in customFiles
            select new object[] { config, templateType, customFile.Key, customFile.Value };
    }
        

    [Theory]
    [MemberData(nameof(FullIcuWithInvariantTestData), parameters: new object[] { "Release" })]
    public async Task FullIcuFromRuntimePackWithInvariant(string config, string templateType, bool aot, bool invariant, bool fullIcu, string testedLocales) =>
        await BuildAndRunIcuTest(
            config,
            templateType,
            aot,
            testedLocales,
            globalizationMode: invariant ? GlobalizationMode.Invariant : fullIcu ? GlobalizationMode.FullIcu : GlobalizationMode.Sharded,
            extraProperties:
                // https://github.com/dotnet/runtime/issues/94133: "wasmbrowser" should use WasmIncludeFullIcuData, not BlazorWebAssemblyLoadAllGlobalizationData
                templateType == "wasmconsole" ?
                $"<InvariantGlobalization>{invariant}</InvariantGlobalization><WasmIncludeFullIcuData>{fullIcu}</WasmIncludeFullIcuData><RunAOTCompilation>{aot}</RunAOTCompilation>" :
                $"<InvariantGlobalization>{invariant}</InvariantGlobalization><BlazorWebAssemblyLoadAllGlobalizationData>{fullIcu}</BlazorWebAssemblyLoadAllGlobalizationData><RunAOTCompilation>{aot}</RunAOTCompilation>");

    [Theory]
    [MemberData(nameof(FullIcuWithICustomIcuTestData), parameters: new object[] { "Release" })]
    public async Task FullIcuFromRuntimePackWithCustomIcu(string config, string templateType, bool aot, bool fullIcu)
    {
        bool isBrowser = templateType == "wasmbrowser";
        string customIcuProperty = isBrowser ? "BlazorIcuDataFileName" : "WasmIcuDataFileName";
        string fullIcuProperty = isBrowser ? "BlazorWebAssemblyLoadAllGlobalizationData" : "WasmIncludeFullIcuData";
        string extraProperties = $"<{customIcuProperty}>{CustomIcuPath}</{customIcuProperty}><{fullIcuProperty}>{fullIcu}</{fullIcuProperty}><RunAOTCompilation>{aot}</RunAOTCompilation>";
        
        string testedLocales = fullIcu ? s_fullIcuTestedLocales : s_customIcuTestedLocales;
        GlobalizationMode globalizationMode = fullIcu ? GlobalizationMode.FullIcu : GlobalizationMode.Custom;
        string customIcuFile = fullIcu ? "" : CustomIcuPath;
        string output = await BuildAndRunIcuTest(config, templateType, aot, testedLocales, globalizationMode, extraProperties, icuFileName: customIcuFile);
        if (fullIcu)
            Assert.Contains($"$({customIcuProperty}) has no effect when $({fullIcuProperty}) is set to true.", output);
    }

    [Theory]
    [MemberData(nameof(IncorrectIcuTestData), parameters: new object[] { "Release" })]
    public void NonExistingCustomFileAssertError(string config, string templateType, string customIcu, bool isFilenameFormCorrect)
    {        
        bool isBrowser = templateType == "wasmbrowser";
        string customIcuProperty = isBrowser ? "BlazorIcuDataFileName" : "WasmIcuDataFileName";
        string extraProperties = $"<{customIcuProperty}>{customIcu}</{customIcuProperty}>";
    
        (BuildArgs buildArgs, string projectFile) = CreateIcuProject(
            config, templateType, aot: false, "Array.Empty<Locale>()", extraProperties);
        string output = BuildIcuTest(
            buildArgs,
            isBrowser,
            GlobalizationMode.Custom,
            customIcu,
            expectSuccess: false,
            assertAppBundle: false);
        
        if (isBrowser)
        {
            if (isFilenameFormCorrect)
            {
                Assert.Contains($"Could not find $({customIcuProperty})={customIcu}, or when used as a path relative to the runtime pack", output);
            }
            else
            {
                Assert.Contains($"File name in $({customIcuProperty}) has to start with 'icudt'.", output);
            }
        }
        else
        {
            // https://github.com/dotnet/runtime/issues/102743: console apps should also require "icudt" at the beginning, unify it
            Assert.Contains($"File in location $({customIcuProperty})={customIcu} cannot be found neither when used as absolute path nor a relative runtime pack path.", output);
        }
    }
}
