// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl object handle methods.
/// Uses the BasicThreads debuggee (heap dump).
/// </summary>
public class DacDbiObjectDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BasicThreads";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetVmObjectHandle_IsIdentity(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ulong testAddr = 0x12345678;
        ulong result;
        int hr = dbi.GetVmObjectHandle(testAddr, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(testAddr, result);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetHandleAddressFromVmHandle_IsIdentity(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ulong testAddr = 0xABCDEF00;
        ulong result;
        int hr = dbi.GetHandleAddressFromVmHandle(testAddr, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(testAddr, result);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetAppDomainIdFromVmObjectHandle_ReturnsOneForNonZero(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        uint id;
        int hr = dbi.GetAppDomainIdFromVmObjectHandle(0x12345678, &id);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(1u, id);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetAppDomainIdFromVmObjectHandle_ReturnsZeroForNull(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        uint id;
        int hr = dbi.GetAppDomainIdFromVmObjectHandle(0, &id);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(0u, id);
    }
}
