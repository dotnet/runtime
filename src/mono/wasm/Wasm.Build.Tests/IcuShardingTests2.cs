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

public class IcuShardingTests2 : IcuTestsBase
{
    public IcuShardingTests2(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) { }

    public static IEnumerable<object?[]> IcuExpectedAndMissingShardFromRuntimePackTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { "icudt.dat",
                                $@"new Locale[] {{
                                    new Locale(""en-GB"", ""{SundayNames.English}""), new Locale(""zh-CN"", ""{SundayNames.Chinese}""), new Locale(""sk-SK"", ""{SundayNames.Slovak}""),
                                    new Locale(""xx-yy"", null) }}" },
                new object[] { "icudt_EFIGS.dat", GetEfigsTestedLocales() },
                new object[] { "icudt_CJK.dat", GetCjkTestedLocales() },
                new object[] { "icudt_no_CJK.dat", GetNocjkTestedLocales() })
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();


    [Theory]
    [MemberData(nameof(IcuExpectedAndMissingShardFromRuntimePackTestData), parameters: new object[] { false, RunHost.NodeJS | RunHost.Chrome })]
    [MemberData(nameof(IcuExpectedAndMissingShardFromRuntimePackTestData), parameters: new object[] { true, RunHost.NodeJS | RunHost.Chrome })]
    public void DefaultAvailableIcuShardsFromRuntimePack(BuildArgs buildArgs, string shardName, string testedLocales, RunHost host, string id) =>
        TestIcuShards(buildArgs, shardName, testedLocales, host, id);
}