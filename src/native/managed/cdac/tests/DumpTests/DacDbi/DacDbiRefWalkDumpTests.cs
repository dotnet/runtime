// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the ref-walking APIs
/// (<see cref="DacDbiImpl.CreateRefWalk"/> / <see cref="DacDbiImpl.WalkRefs"/> /
/// <see cref="DacDbiImpl.DeleteRefWalk"/>), cross-validated against the GC handle table
/// and the stack-reference walk in the GCRoots debuggee.
/// </summary>
public class DacDbiRefWalkDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "GCRoots";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    /// <summary>
    /// Drives <see cref="DacDbiImpl.WalkRefs"/> to completion and returns every reference reported.
    /// </summary>
    private static unsafe List<DacGcReference> WalkAllRefs(DacDbiImpl dbi, bool walkStacks, CorGCReferenceType handleWalkMask, uint batchSize = 32)
    {
        List<DacGcReference> refs = new();

        nuint handle = 0;
        int hr = dbi.CreateRefWalk(&handle, walkStacks ? Interop.BOOL.TRUE : Interop.BOOL.FALSE, handleWalkMask);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.True(handle != 0, "CreateRefWalk produced a null handle");

        try
        {
            DacGcReference[] buffer = new DacGcReference[batchSize];
            while (true)
            {
                uint fetched = 0;
                int walkHr;
                fixed (DacGcReference* bufPtr = buffer)
                {
                    walkHr = dbi.WalkRefs(handle, batchSize, bufPtr, &fetched);
                }

                Assert.True(
                    walkHr == System.HResults.S_OK || walkHr == System.HResults.S_FALSE,
                    $"WalkRefs returned 0x{walkHr:x}");

                for (uint i = 0; i < fetched; i++)
                    refs.Add(buffer[i]);

                if (walkHr == System.HResults.S_FALSE)
                    break;
            }
        }
        finally
        {
            int delHr = dbi.DeleteRefWalk(handle);
            Assert.Equal(System.HResults.S_OK, delHr);
        }

        return refs;
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void WalkRefs_StrongHandles_MatchHandleTable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IGC gc = Target.Contracts.GC;

        List<DacGcReference> refs = WalkAllRefs(dbi, walkStacks: false, handleWalkMask: CorGCReferenceType.CorHandleStrong);

        // Every reference must be a strong handle reported by its (low-bit-clear) handle address.
        HashSet<ulong> walkedHandles = new();
        foreach (DacGcReference r in refs)
        {
            Assert.Equal(CorGCReferenceType.CorHandleStrong, r.dwType);
            Assert.Equal(0ul, r.pObject & 1);
            walkedHandles.Add(r.pObject);
        }

        HashSet<ulong> expectedHandles = gc.GetHandles([HandleType.Strong]).Select(h => h.Handle.Value).ToHashSet();
        Assert.True(expectedHandles.Count > 0, "Expected at least one strong handle in GCRoots.");
        Assert.Equal(expectedHandles, walkedHandles);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void WalkRefs_StacksOnly_MatchStackReferenceWalk(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IThread threadContract = Target.Contracts.Thread;
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        List<DacGcReference> refs = WalkAllRefs(dbi, walkStacks: true, handleWalkMask: (CorGCReferenceType)0);

        // Compute the expected count of stack references directly (excluding cDAC-private
        // deferred-frame markers, which WalkRefs filters out).
        // Mirrors StackWalkHelpers.GcScanFlags.CDAC_DEFERRED_FRAME (internal to the Contracts assembly).
        const uint CdacDeferredFrame = 0x40000000;
        int expected = 0;
        ThreadStoreData threadStore = threadContract.GetThreadStoreData();
        TargetPointer threadAddr = threadStore.FirstThread;
        while (threadAddr != TargetPointer.Null)
        {
            ThreadData td = threadContract.GetThreadData(threadAddr);
            expected += stackWalk.WalkStackReferences(td).Count(r => (r.Flags & CdacDeferredFrame) == 0);
            threadAddr = td.NextThread;
        }

        foreach (DacGcReference r in refs)
            Assert.Equal(CorGCReferenceType.CorReferenceStack, r.dwType);

        Assert.Equal(expected, refs.Count);
    }
}
