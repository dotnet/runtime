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

    public static IEnumerable<object[]> IcuExpectedAndMissingCustomShardTestData(Configuration config) =>
        from aot in boolOptions
            from onlyPredefinedCultures in boolOptions
            // isOnlyPredefinedCultures = true fails with wasmbrowser: https://github.com/dotnet/runtime/issues/108272
            where !(onlyPredefinedCultures)
            select new object[] { config, aot, CustomIcuPath, s_customIcuTestedLocales, onlyPredefinedCultures };

    public static IEnumerable<object[]> IcuExpectedAndMissingAutomaticShardTestData(Configuration config)
    {
        var locales = new Dictionary<string, string>
        {
            { "fr-FR", GetEfigsTestedLocales(SundayNames.French) },
            { "ja-JP", GetCjkTestedLocales(SundayNames.Japanese) },
            { "sk-SK", GetNocjkTestedLocales(SundayNames.Slovak) }
        }; 
        return from aot in boolOptions
            from locale in locales
            select new object[] { config, aot, locale.Key, locale.Value };
    }

    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingCustomShardTestData), parameters: new object[] { Configuration.Release })]
    public async Task CustomIcuShard(Configuration config, bool aot, string customIcuPath, string customLocales, bool onlyPredefinedCultures) =>
        await TestIcuShards(config, Template.WasmBrowser, aot, customIcuPath, customLocales, GlobalizationMode.Custom, onlyPredefinedCultures);

    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingAutomaticShardTestData), parameters: new object[] { Configuration.Release })]
    public async Task AutomaticShardSelectionDependingOnEnvLocale(Configuration config, bool aot, string environmentLocale, string testedLocales) =>
        await PublishAndRunIcuTest(config, Template.WasmBrowser, aot, testedLocales, GlobalizationMode.Sharded, locale: environmentLocale);
}
