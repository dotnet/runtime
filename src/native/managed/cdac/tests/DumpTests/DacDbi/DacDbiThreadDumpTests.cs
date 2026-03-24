// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl thread methods.
/// Uses the BasicThreads debuggee (heap dump), which spawns 5 named threads then crashes.
/// </summary>
public class DacDbiThreadDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BasicThreads";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void EnumerateThreads_CountMatchesContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        int dbiCount = 0;
        delegate* unmanaged<ulong, nint, void> callback = &CountThreadCallback;
        int hr = dbi.EnumerateThreads((nint)callback, (nint)(&dbiCount));
        Assert.Equal(System.HResults.S_OK, hr);

        int expectedCount = 0;
        TargetPointer current = storeData.FirstThread;
        while (current != TargetPointer.Null)
        {
            ThreadData data = threadContract.GetThreadData(current);
            bool isDead = (data.State & ThreadState.Dead) != 0;
            bool isUnstarted = (data.State & ThreadState.Unstarted) != 0;
            if (!isDead && !isUnstarted)
            {
                expectedCount++;
            }

            current = data.NextThread;
        }

        Assert.Equal(expectedCount, dbiCount);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void IsThreadMarkedDead_CrossValidateWithContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        TargetPointer current = storeData.FirstThread;
        while (current != TargetPointer.Null)
        {
            Interop.BOOL isDead;
            int hr = dbi.IsThreadMarkedDead(current, &isDead);
            Assert.Equal(System.HResults.S_OK, hr);

            ThreadData data = threadContract.GetThreadData(current);
            bool contractSaysDead = (data.State & ThreadState.Dead) != 0;
            Assert.Equal(contractSaysDead, isDead == Interop.BOOL.TRUE);

            current = data.NextThread;
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void TryGetVolatileOSThreadID_MatchesContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        TargetPointer current = storeData.FirstThread;
        while (current != TargetPointer.Null)
        {
            uint osId;
            int hr = dbi.TryGetVolatileOSThreadID(current, &osId);
            Assert.Equal(System.HResults.S_OK, hr);

            ThreadData data = threadContract.GetThreadData(current);
            // DacDbi normalizes SWITCHED_OUT_FIBER_OSID (0xbaadf00d) to 0
            const uint SWITCHED_OUT_FIBER_OSID = 0xbaadf00d;
            uint expectedOsId = (uint)data.OSId.Value;
            if (expectedOsId == SWITCHED_OUT_FIBER_OSID)
                expectedOsId = 0;
            Assert.Equal(expectedOsId, osId);

            current = data.NextThread;
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetUniqueThreadID_MatchesContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        TargetPointer current = storeData.FirstThread;
        HashSet<uint> seenIds = new();

        while (current != TargetPointer.Null)
        {
            uint uniqueId;
            int hr = dbi.GetUniqueThreadID(current, &uniqueId);
            Assert.Equal(System.HResults.S_OK, hr);

            ThreadData data = threadContract.GetThreadData(current);
            Assert.Equal((uint)data.OSId.Value, uniqueId);
            Assert.True(seenIds.Add(uniqueId), $"Duplicate thread ID: {uniqueId}");

            current = data.NextThread;
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetCurrentException_CrossValidateWithContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        TargetPointer current = storeData.FirstThread;
        int checkedCount = 0;

        while (current != TargetPointer.Null)
        {
            ulong exception;
            int hr = dbi.GetCurrentException(current, &exception);
            Assert.Equal(System.HResults.S_OK, hr);

            TargetPointer contractException = threadContract.GetThrowableObject(current);
            Assert.Equal(contractException.Value, exception);
            checkedCount++;

            ThreadData data = threadContract.GetThreadData(current);
            current = data.NextThread;
        }

        Assert.True(checkedCount > 0, "Should have checked at least one thread");
    }

    [UnmanagedCallersOnly]
    private static unsafe void CountThreadCallback(ulong addr, nint userData)
    {
        (*(int*)userData)++;
    }
}
