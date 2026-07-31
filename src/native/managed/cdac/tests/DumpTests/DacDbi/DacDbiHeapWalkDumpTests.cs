// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the new heap walking APIs
/// (<see cref="DacDbiImpl.CreateHeapWalk"/> / <see cref="DacDbiImpl.WalkHeap"/> /
/// <see cref="DacDbiImpl.DeleteHeapWalk"/>), cross-validated against handle-rooted
/// objects in the GCRoots debuggee.
/// </summary>
public class DacDbiHeapWalkDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "GCRoots";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    /// <summary>
    /// Drives <see cref="DacDbiImpl.WalkHeap"/> to completion and returns every
    /// object reported, keyed by address.
    /// </summary>
    private static unsafe Dictionary<ulong, COR_HEAPOBJECT> WalkAllObjects(DacDbiImpl dbi, uint batchSize = 256)
    {
        Dictionary<ulong, COR_HEAPOBJECT> objects = new();

        nuint handle = 0;
        int hr = dbi.CreateHeapWalk(&handle);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.True(handle != 0, "CreateHeapWalk produced a null handle");

        try
        {
            COR_HEAPOBJECT[] buffer = new COR_HEAPOBJECT[batchSize];
            while (true)
            {
                uint fetched = 0;
                int walkHr;
                fixed (COR_HEAPOBJECT* bufPtr = buffer)
                {
                    walkHr = dbi.WalkHeap(handle, batchSize, bufPtr, &fetched);
                }

                Assert.True(
                    walkHr == System.HResults.S_OK || walkHr == System.HResults.S_FALSE || walkHr == System.HResults.E_FAIL,
                    $"WalkHeap returned 0x{walkHr:x}");

                for (uint i = 0; i < fetched; i++)
                    objects[buffer[i].address] = buffer[i];

                if (walkHr == System.HResults.S_FALSE)
                    break;
                if (fetched == 0)
                    break;
            }
        }
        finally
        {
            int delHr = dbi.DeleteHeapWalk(handle);
            Assert.Equal(System.HResults.S_OK, delHr);
        }

        return objects;
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void WalkHeap_StrongHandleTargets_AppearInHeapWalk(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IGC gc = Target.Contracts.GC;
        IObject objContract = Target.Contracts.Object;

        Dictionary<ulong, COR_HEAPOBJECT> heap = WalkAllObjects(dbi);

        int matched = 0;
        foreach (HandleData h in gc.GetHandles([HandleType.Strong]))
        {
            TargetPointer objAddr = Target.ReadPointer(h.Handle);
            if (objAddr == TargetPointer.Null)
                continue;

            // Match by address — strongly authoritative.
            Assert.True(heap.TryGetValue(objAddr.Value, out COR_HEAPOBJECT walked),
                $"Strong-handle target 0x{objAddr:X} not found in heap walk");

            // Cross-check the MT we get from the object header against the MT
            // WalkHeap reported. They must agree.
            TargetPointer mtFromHeader = objContract.GetMethodTableAddress(objAddr);
            Assert.Equal(mtFromHeader.Value, walked.type.token1);
            matched++;
        }

        Assert.True(matched > 0, "Expected at least one strong handle to resolve to a heap object.");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void WalkHeap_PinnedHandleTargets_AppearInHeapWalk(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IGC gc = Target.Contracts.GC;

        Dictionary<ulong, COR_HEAPOBJECT> heap = WalkAllObjects(dbi);

        // The GCRoots debuggee allocates 5 pinned byte[64] arrays
        // (Program.PinnedObjectCount).
        HashSet<ulong> pinnedAddrs = new();
        HashSet<ulong> pinnedMts = new();
        foreach (HandleData h in gc.GetHandles([HandleType.Pinned]))
        {
            TargetPointer objAddr = Target.ReadPointer(h.Handle);
            if (objAddr == TargetPointer.Null)
                continue;

            Assert.True(heap.TryGetValue(objAddr.Value, out COR_HEAPOBJECT walked),
                $"Pinned-handle target 0x{objAddr:X} not found in heap walk");

            pinnedAddrs.Add(objAddr.Value);
            pinnedMts.Add(walked.type.token1);
        }

        Assert.True(pinnedAddrs.Count >= 5,
            $"Expected at least 5 pinned byte[] arrays, found {pinnedAddrs.Count}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void WalkHeap_FindsTestStringByContent_AddressMatchesStrongHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IGC gc = Target.Contracts.GC;
        IObject objContract = Target.Contracts.Object;

        // GCRoots/Program.cs::TestStringValue
        const string Marker = "cDAC-GCRoots-test-string";

        // 1) Find the marker via the handle path: scan strong handles and
        //    GetStringValue each target until we hit the marker.
        TargetPointer fromHandle = TargetPointer.Null;
        foreach (HandleData h in gc.GetHandles([HandleType.Strong]))
        {
            TargetPointer addr = Target.ReadPointer(h.Handle);
            if (addr == TargetPointer.Null)
                continue;
            try
            {
                if (objContract.GetStringValue(addr) == Marker)
                {
                    fromHandle = addr;
                    break;
                }
            }
            catch
            {
                // Not a string or unreadable — keep going.
            }
        }
        Assert.NotEqual(TargetPointer.Null, fromHandle);

        // 2) Find the marker via the heap walk: filter to String MT, then read
        //    the value of each candidate.
        TargetPointer stringMT = Target.ReadPointer(Target.ReadGlobalPointer("StringMethodTable"));
        TargetPointer fromHeapWalk = TargetPointer.Null;
        foreach (COR_HEAPOBJECT o in WalkAllObjects(dbi).Values)
        {
            if (o.type.token1 != stringMT.Value)
                continue;
            try
            {
                if (objContract.GetStringValue(new TargetPointer(o.address)) == Marker)
                {
                    fromHeapWalk = new TargetPointer(o.address);
                    break;
                }
            }
            catch
            {
                // Skip strings we can't read.
            }
        }
        Assert.NotEqual(TargetPointer.Null, fromHeapWalk);

        // 3) Both paths must agree on the address.
        Assert.Equal(fromHandle.Value, fromHeapWalk.Value);
    }
}
