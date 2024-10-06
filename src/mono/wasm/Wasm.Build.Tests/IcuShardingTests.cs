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

    public static IEnumerable<object[]> IcuExpectedAndMissingCustomShardTestData(string config) =>
        from templateType in templateTypes
            from aot in boolOptions
            from onlyPredefinedCultures in boolOptions
            // isOnlyPredefinedCultures = true fails with wasmbrowser: https://github.com/dotnet/runtime/issues/108272
            where !(onlyPredefinedCultures && templateType == "wasmbrowser")
            select new object[] { config, templateType, aot, CustomIcuPath, s_customIcuTestedLocales, onlyPredefinedCultures };

    public static IEnumerable<object[]> IcuExpectedAndMissingAutomaticShardTestData(string config)
    {
        var locales = new Dictionary<string, string>
        {
            { "fr-FR", GetEfigsTestedLocales(SundayNames.French) },
            { "ja-JP", GetCjkTestedLocales(SundayNames.Japanese) },
            { "sk-SK", GetNocjkTestedLocales(SundayNames.Slovak) }
        }; 
        // "wasmconsole": https://github.com/dotnet/runtime/issues/82593
        return from aot in boolOptions
            from locale in locales
            select new object[] { config, "wasmbrowser", aot, locale.Key, locale.Value };
    }

    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingCustomShardTestData), parameters: new object[] { "Release" })]
    public async Task CustomIcuShard(string config, string templateType, bool aot, string customIcuPath, string customLocales, bool onlyPredefinedCultures) =>
        await TestIcuShards(config, templateType, aot, customIcuPath, customLocales, GlobalizationMode.Custom, onlyPredefinedCultures);

    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingAutomaticShardTestData), parameters: new object[] { "Release" })]
    public async Task AutomaticShardSelectionDependingOnEnvLocale(string config, string templateType, bool aot, string environmentLocale, string testedLocales) =>
        await BuildAndRunIcuTest(config, templateType, aot, testedLocales, GlobalizationMode.Sharded, language: environmentLocale);
}
