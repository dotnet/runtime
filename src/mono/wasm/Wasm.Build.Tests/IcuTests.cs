// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace Wasm.Build.Tests;

public class IcuTests : IcuTestsBase
{
    public IcuTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) { }

    public static IEnumerable<object?[]> FullIcuWithInvariantTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                // in invariant mode, all locales should be missing
                new object[] { true, true, "Array.Empty<Locale>()" },
                new object[] { true, false, "Array.Empty<Locale>()" },
                new object[] { false, false, GetEfigsTestedLocales() },
                new object[] { false, true,  s_fullIcuTestedLocales})
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    public static IEnumerable<object?[]> FullIcuWithICustomIcuTestData(bool aot, RunHost host)
        => ConfigWithAOTData(aot)
            .Multiply(
                new object[] { true },
                new object[] { false })
            .WithRunHosts(host)
            .UnwrapItemsAsArrays();

    [Theory]
    [MemberData(nameof(FullIcuWithInvariantTestData), parameters: new object[] { false, RunHost.NodeJS | RunHost.Chrome })]
    [MemberData(nameof(FullIcuWithInvariantTestData), parameters: new object[] { true, RunHost.NodeJS | RunHost.Chrome })]
    public async Task FullIcuFromRuntimePackWithInvariantAsync(BuildArgs buildArgs, bool invariant, bool fullIcu, string testedLocales, RunHost host, string id)
    {
        string projectName = $"fullIcuInvariant_{fullIcu}_{invariant}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        buildArgs = ExpandBuildArgs(buildArgs, extraProperties: $"<InvariantGlobalization>{invariant}</InvariantGlobalization><WasmIncludeFullIcuData>{fullIcu}</WasmIncludeFullIcuData>");

        string programText = GetProgramText(testedLocales);
        _testOutput.WriteLine($"----- Program: -----{Environment.NewLine}{programText}{Environment.NewLine}-------");
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                            GlobalizationMode: invariant ? GlobalizationMode.Invariant : fullIcu ? GlobalizationMode.FullIcu : GlobalizationMode.Sharded));

        await RunAndTestWasmAppAsync(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
    }

    [Theory]
    [MemberData(nameof(FullIcuWithICustomIcuTestData), parameters: new object[] { false, RunHost.NodeJS | RunHost.Chrome })]
    [MemberData(nameof(FullIcuWithICustomIcuTestData), parameters: new object[] { true, RunHost.NodeJS | RunHost.Chrome })]
    public async Task FullIcuFromRuntimePackWithCustomIcuAsync(BuildArgs buildArgs, bool fullIcu, RunHost host, string id)
    {
        string projectName = $"fullIcuCustom_{fullIcu}_{buildArgs.Config}_{buildArgs.AOT}";
        bool dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

        buildArgs = buildArgs with { ProjectName = projectName };
        buildArgs = ExpandBuildArgs(buildArgs, extraProperties: $"<WasmIcuDataFileName>{CustomIcuPath}</WasmIcuDataFileName><WasmIncludeFullIcuData>{fullIcu}</WasmIncludeFullIcuData>");

        string testedLocales = fullIcu ? s_fullIcuTestedLocales : s_customIcuTestedLocales;
        string programText = GetProgramText(testedLocales);
        _testOutput.WriteLine($"----- Program: -----{Environment.NewLine}{programText}{Environment.NewLine}-------");
        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                            DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                            GlobalizationMode: fullIcu ? GlobalizationMode.FullIcu : GlobalizationMode.PredefinedIcu,
                            PredefinedIcudt: fullIcu ? "" : CustomIcuPath));
        if (fullIcu)
            Assert.Contains("$(WasmIcuDataFileName) has no effect when $(WasmIncludeFullIcuData) is set to true.", output);

        await RunAndTestWasmAppAsync(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
    }

    [Theory]
    [BuildAndRun(host: RunHost.None, parameters: new object[] { "icudtNonExisting.dat", true })]
    [BuildAndRun(host: RunHost.None, parameters: new object[] { "incorrectName.dat", false })]
    public void NonExistingCustomFileAssertError(BuildArgs buildArgs, string customFileName, bool isFilenameCorrect, string id)
    {
        string projectName = $"invalidCustomIcu_{buildArgs.Config}_{buildArgs.AOT}";
        buildArgs = buildArgs with { ProjectName = projectName };
        string customIcu = Path.Combine(BuildEnvironment.TestAssetsPath, customFileName);
        buildArgs = ExpandBuildArgs(buildArgs, extraProperties: $"<WasmIcuDataFileName>{customIcu}</WasmIcuDataFileName>");

        (_, string output) = BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                            ExpectSuccess: false));
        if (isFilenameCorrect)
        {
            Assert.Contains($"File in location $(WasmIcuDataFileName)={customIcu} cannot be found neither when used as absolute path nor a relative runtime pack path.", output);
        }
        else
        {
            Assert.Contains($"Custom ICU file name in path $(WasmIcuDataFileName)={customIcu} must start with 'icudt'.", output);
        }
    }
}
