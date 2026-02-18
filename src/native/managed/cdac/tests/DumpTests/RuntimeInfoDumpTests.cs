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
public abstract class RuntimeInfoDumpTestsBase : DumpTestBase
{
    protected RuntimeInfoDumpTestsBase()
    {
        LoadDump();
    }

    protected override string DebuggeeName => "BasicThreads";

    [Fact]
    public void RuntimeInfo_ContractIsAvailable()
    {
        IRuntimeInfo runtimeInfo = Target.Contracts.RuntimeInfo;
        Assert.NotNull(runtimeInfo);
    }

    [Fact]
    public void RuntimeInfo_ArchitectureIsValid()
    {
        IRuntimeInfo runtimeInfo = Target.Contracts.RuntimeInfo;
        RuntimeInfoArchitecture arch = runtimeInfo.GetTargetArchitecture();

        Assert.True(Enum.IsDefined(arch),
            $"Expected a valid RuntimeInfoArchitecture enum value, got {arch}");
    }

    [Fact]
    public void RuntimeInfo_OperatingSystemIsValid()
    {
        IRuntimeInfo runtimeInfo = Target.Contracts.RuntimeInfo;
        RuntimeInfoOperatingSystem os = runtimeInfo.GetTargetOperatingSystem();

        Assert.True(Enum.IsDefined(os),
            $"Expected a valid RuntimeInfoOperatingSystem enum value, got {os}");
    }
}

public class RuntimeInfoDumpTests_Local : RuntimeInfoDumpTestsBase
{
    protected override string RuntimeVersion => "local";
}

public class RuntimeInfoDumpTests_Net10 : RuntimeInfoDumpTestsBase
{
    protected override string RuntimeVersion => "net10.0";
}
