// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class NonWasmTemplateBuildTests : TestMainJsTestBase
{
    public NonWasmTemplateBuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    // For building non-wasm project with the sdk installed, we need to
    // patch the framework references. But we want to maintain the versions.
    // So, copy the reference for latest TFM, and add that back with the
    // TFM=DefaultTargetFramework
    //
    // This is useful for the case when we are on tfm=net7.0, but sdk, and packages
    // are really 8.0 .
    private const string s_latestTargetFramework = "net9.0";
    private const string s_previousTargetFramework = "net8.0";
    private static string s_directoryBuildTargetsForPreviousTFM =
        $$"""
            <Project>
              <Target Name="_FixupVersions" BeforeTargets="ProcessFrameworkReferences" Condition="'{{s_latestTargetFramework}}' != '{{DefaultTargetFramework}}'">
                <ItemGroup>
                  <!-- Get {{s_latestTargetFramework}} entry -->
                  <_KnownFrameworkReferenceToCopyFrom
                          Include="@(KnownFrameworkReference)"
                          Condition="'%(Identity)' == 'Microsoft.NETCore.App' and '%(TargetFramework)' == '{{s_latestTargetFramework}}'" />
                  <!-- patch it's TFM={{DefaultTargetFramework}} -->
                  <_KnownFrameworkReferenceToCopyFrom Update="@(_KnownFrameworkReferenceToCopyFrom)" TargetFramework="{{DefaultTargetFramework}}" />

                  <!-- remove the existing {{DefaultTargetFramework}} entry -->
                  <KnownFrameworkReference
                          Remove="@(KnownFrameworkReference)"
                          Condition="'%(Identity)' == 'Microsoft.NETCore.App' and '%(TargetFramework)' == '{{DefaultTargetFramework}}'" />
                  <!-- add the new patched up {{DefaultTargetFramework}} entry -->
                  <KnownFrameworkReference Include="@(_KnownFrameworkReferenceToCopyFrom)" />
                </ItemGroup>
              </Target>
            </Project>
        """;

    private static string s_directoryBuildTargetsForCurrentTFM = "<Project />";

    public static IEnumerable<object?[]> GetTestData() =>
        new IEnumerable<object?>[]
        {
            new object?[] { "Debug" },
            new object?[] { "Release" }
        }
        .AsEnumerable()
        .MultiplyWithSingleArgs
        (
            "",
            "/p:RunAOTCompilation=true",
            "/p:WasmBuildNative=true"
        )
        .MultiplyWithSingleArgs
        (
            "net6.0",
            s_previousTargetFramework,
            s_latestTargetFramework
        )
        .UnwrapItemsAsArrays().ToList();

    [Theory, TestCategory("no-workload")]
    [MemberData(nameof(GetTestData))]
    public void NonWasmConsoleBuild_WithoutWorkload(string config, string extraBuildArgs, string targetFramework)
        => NonWasmConsoleBuild(config,
                               extraBuildArgs,
                               targetFramework,
                               // net6 is sdk would be needed to run the app
                               shouldRun: targetFramework == s_latestTargetFramework);

    [Theory]
    [MemberData(nameof(GetTestData))]
    public void NonWasmConsoleBuild_WithWorkload(string config, string extraBuildArgs, string targetFramework)
        => NonWasmConsoleBuild(config,
                               extraBuildArgs,
                               targetFramework,
                               // net6 is sdk would be needed to run the app
                               shouldRun: targetFramework == s_latestTargetFramework);

    private void NonWasmConsoleBuild(string config,
                                     string extraBuildArgs,
                                     string targetFramework,
                                     string? directoryBuildTargets = null,
                                     bool shouldRun = true)
    {
        string id = $"nonwasm_{targetFramework}_{config}_{GetRandomId()}";
        InitPaths(id);
        InitProjectDir(_projectDir);

        directoryBuildTargets ??= targetFramework == s_previousTargetFramework
                                    ? s_directoryBuildTargetsForPreviousTFM
                                    : s_directoryBuildTargetsForCurrentTFM;

        File.WriteAllText(Path.Combine(_projectDir, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(_projectDir, "Directory.Build.targets"), directoryBuildTargets);

        new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false)
                .WithWorkingDirectory(_projectDir!)
                .ExecuteWithCapturedOutput("new console --no-restore")
                .EnsureSuccessful();

        new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false)
                .WithWorkingDirectory(_projectDir!)
                .ExecuteWithCapturedOutput($"build -restore -c {config} -bl:{Path.Combine(s_buildEnv.LogRootPath, $"{id}.binlog")} {extraBuildArgs} -f {targetFramework}")
                .EnsureSuccessful();

        if (shouldRun)
        {
            var result = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false)
                                .WithWorkingDirectory(_projectDir!)
                                .ExecuteWithCapturedOutput($"run -c {config} -f {targetFramework} --no-build")
                                .EnsureSuccessful();

            Assert.Contains("Hello, World!", result.Output);
        }
    }
}
