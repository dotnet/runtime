// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl AppDomain, misc policy, and simple thread
/// property methods. Uses the BasicThreads debuggee (heap dump).
/// </summary>
public class DacDbiAppDomainDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BasicThreads";
    protected override string DumpType => "full";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetAppDomainId_ReturnsOneForValidAppDomain(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);

        uint id;
        int hr = dbi.GetAppDomainId(appDomain, &id);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(1u, id);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetAppDomainId_ReturnsZeroForNull(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        uint id;
        int hr = dbi.GetAppDomainId(0, &id);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(0u, id);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetCurrentAppDomain_ReturnsNonNull(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ulong appDomain;
        int hr = dbi.GetCurrentAppDomain(&appDomain);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.NotEqual(0UL, appDomain);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetConnectionID_ReturnsZero(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        uint connId;
        int hr = dbi.GetConnectionID(storeData.FirstThread, &connId);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(0u, connId);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetTaskID_ReturnsZero(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        ulong taskId;
        int hr = dbi.GetTaskID(storeData.FirstThread, &taskId);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(0UL, taskId);
    }
}
