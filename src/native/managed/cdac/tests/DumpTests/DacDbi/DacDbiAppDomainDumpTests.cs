// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
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
    public unsafe void GetAppDomainFromId_ReturnsAppDomain(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ulong appDomain;
        int hr = dbi.GetAppDomainFromId(1, &appDomain);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.NotEqual(0UL, appDomain);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetAppDomainFromId_FailsForInvalidId(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ulong appDomain;
        int hr = dbi.GetAppDomainFromId(99, &appDomain);
        Assert.True(hr < 0, "Expected failure HRESULT for invalid AppDomain ID");
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
    public unsafe void GetCurrentAppDomain_MatchesGetAppDomainFromId(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ulong currentAD;
        int hr1 = dbi.GetCurrentAppDomain(&currentAD);
        Assert.Equal(System.HResults.S_OK, hr1);

        ulong fromId;
        int hr2 = dbi.GetAppDomainFromId(1, &fromId);
        Assert.Equal(System.HResults.S_OK, hr2);

        Assert.Equal(currentAD, fromId);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void EnumerateAppDomains_CallsCallbackOnce(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        int count = 0;
        delegate* unmanaged<ulong, nint, void> callback = &CountCallback;
        int hr = dbi.EnumerateAppDomains((nint)callback, (nint)(&count));
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(1, count);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void IsAssemblyFullyTrusted_ReturnsTrue(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ILoader loader = Target.Contracts.Loader;
        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);
        IEnumerable<ModuleHandle> modules = loader.GetModuleHandles(new TargetPointer(appDomain),
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

        foreach (ModuleHandle module in modules)
        {
            TargetPointer moduleAddr = loader.GetModule(module);
            Interop.BOOL result;
            int hr = dbi.IsAssemblyFullyTrusted(moduleAddr, &result);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(Interop.BOOL.TRUE, result);
            break;
        }
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

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void IsWinRTModule_ReturnsFalse(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ILoader loader = Target.Contracts.Loader;
        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);
        IEnumerable<ModuleHandle> modules = loader.GetModuleHandles(new TargetPointer(appDomain),
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

        foreach (ModuleHandle module in modules)
        {
            TargetPointer moduleAddr = loader.GetModule(module);
            Interop.BOOL isWinRT;
            int hr = dbi.IsWinRTModule(moduleAddr, &isWinRT);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(Interop.BOOL.FALSE, isWinRT);
            break;
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void EnableNGENPolicy_ReturnsENotImpl(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        int hr = dbi.EnableNGENPolicy(0);
        Assert.Equal(System.HResults.E_NOTIMPL, hr);
    }

    [UnmanagedCallersOnly]
    private static unsafe void CountCallback(ulong addr, nint userData)
    {
        (*(int*)userData)++;
    }
}
