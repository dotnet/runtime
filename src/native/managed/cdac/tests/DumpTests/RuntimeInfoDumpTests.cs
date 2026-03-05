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
    public void RuntimeInfo_ArchitectureMatchesDumpMetadata(TestConfiguration config)
    {
        InitializeDumpTest(config);
        Assert.NotNull(DumpMetadata);

        IRuntimeInfo runtimeInfo = Target.Contracts.RuntimeInfo;
        RuntimeInfoArchitecture arch = runtimeInfo.GetTargetArchitecture();

        RuntimeInfoArchitecture expected = DumpMetadata.Arch switch
        {
            "x64" => RuntimeInfoArchitecture.X64,
            "x86" => RuntimeInfoArchitecture.X86,
            "arm64" => RuntimeInfoArchitecture.Arm64,
            "arm" => RuntimeInfoArchitecture.Arm,
            _ => RuntimeInfoArchitecture.Unknown,
        };

        Assert.Equal(expected, arch);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void RuntimeInfo_OperatingSystemMatchesDumpMetadata(TestConfiguration config)
    {
        InitializeDumpTest(config);
        Assert.NotNull(DumpMetadata);

        IRuntimeInfo runtimeInfo = Target.Contracts.RuntimeInfo;
        RuntimeInfoOperatingSystem os = runtimeInfo.GetTargetOperatingSystem();

        RuntimeInfoOperatingSystem expected = DumpMetadata.Os switch
        {
            "windows" => RuntimeInfoOperatingSystem.Windows,
            "linux" or "osx" or "freebsd" => RuntimeInfoOperatingSystem.Unix,
            _ => RuntimeInfoOperatingSystem.Unknown,
        };

        Assert.Equal(expected, os);
    }
}
