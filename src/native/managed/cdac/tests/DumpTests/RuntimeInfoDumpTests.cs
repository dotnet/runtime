// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the RuntimeInfo contract.
/// Uses the BasicThreads debuggee dump (any dump works for these tests).
/// </summary>
public class RuntimeInfoDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BasicThreads";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void RuntimeInfo_ContractIsAvailable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IRuntimeInfo runtimeInfo = Target.Contracts.RuntimeInfo;
        Assert.NotNull(runtimeInfo);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void RuntimeInfo_ArchitectureIsValid(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IRuntimeInfo runtimeInfo = Target.Contracts.RuntimeInfo;
        RuntimeInfoArchitecture arch = runtimeInfo.GetTargetArchitecture();

        Assert.True(Enum.IsDefined(arch),
            $"Expected a valid RuntimeInfoArchitecture enum value, got {arch}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void RuntimeInfo_OperatingSystemIsValid(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IRuntimeInfo runtimeInfo = Target.Contracts.RuntimeInfo;
        RuntimeInfoOperatingSystem os = runtimeInfo.GetTargetOperatingSystem();

        Assert.True(Enum.IsDefined(os),
            $"Expected a valid RuntimeInfoOperatingSystem enum value, got {os}");
    }
}
