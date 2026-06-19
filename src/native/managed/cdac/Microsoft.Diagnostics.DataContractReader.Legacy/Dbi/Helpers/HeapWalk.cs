// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal sealed class HeapWalk : IEnum<COR_HEAPOBJECT>
{
    private readonly Target _target;
    private readonly IGC _gc;
    private readonly IRuntimeTypeSystem _rts;
    private readonly TargetPointer _freeObjectMT;
    private readonly uint _numComponentsOffsetArray;
    private readonly uint _numComponentsOffsetString;
    private readonly uint _methodTableOffset;
    private readonly ulong _methodTableMask;

    public IEnumerator<COR_HEAPOBJECT> Enumerator { get; }
    public nuint LegacyHandle { get; set; } = 0;

    public HeapWalk(Target target)
    {
        _target = target;
        _gc = target.Contracts.GC;
        _rts = target.Contracts.RuntimeTypeSystem;
        _freeObjectMT = _rts.GetWellKnownMethodTable(WellKnownMethodTable.Free);
        // use these fields directly instead of through RuntimeTypeSystem so that the heap walk
        // can amortize reads through the active cache scope (see Walk).
        _numComponentsOffsetArray = (uint)target.GetTypeInfo(DataType.Array).Fields[Constants.FieldNames.Array.NumComponents].Offset;
        _numComponentsOffsetString = (uint)target.GetTypeInfo(DataType.String).Fields["m_StringLength"].Offset;
        _methodTableOffset = (uint)target.GetTypeInfo(DataType.Object).Fields["m_pMethTab"].Offset;
        _methodTableMask = (ulong)~target.ReadGlobal<byte>(Constants.Globals.ObjectToMethodTableUnmask);

        Enumerator = Walk().GetEnumerator();
    }

    private IEnumerable<COR_HEAPOBJECT> Walk()
    {
        // Hold a linear-page cache for the entire walk: each iteration tends to touch the same
        // page (object header + a trailing component-count field), so a single cache scope
        // yields a far higher hit rate than activating per-read.
        using IDisposable _ = _target.BeginCacheScope(new LinearReadCache());

        bool pendingFailure = false;
        foreach ((GCHeapSegmentInfo seg, GCHeapData _) in EnumerateAllSegments())
        {
            if (seg.Start.Value >= seg.End.Value)
                continue;

            TargetPointer currentObj = _gc.GetPotentialNextObjectAddress(seg.Start, 0, seg);
            while (currentObj.Value < seg.End.Value)
            {
                if (!_target.TryReadPointer(currentObj.Value + _methodTableOffset, out TargetPointer mt))
                {
                    pendingFailure = true;
                    break;
                }
                mt = new TargetPointer(mt.Value & _methodTableMask);

                if (!TryGetObjectSize(currentObj, mt, out ulong size) || size == 0)
                {
                    pendingFailure = true;
                    break;
                }

                size = _gc.AlignObjectSize(size, seg.Generation);
                if (currentObj.Value + size > seg.End.Value)
                {
                    pendingFailure = true;
                    break;
                }

                // Advance past this object (and any allocation-context tail) in a single call.
                TargetPointer nextObj = _gc.GetPotentialNextObjectAddress(currentObj, size, seg);
                if (nextObj.Value > seg.End.Value)
                {
                    break;
                }

                TargetPointer reportedAddr = currentObj;
                bool isFree = mt == _freeObjectMT;

                currentObj = nextObj;

                if (isFree)
                    continue;

                if (pendingFailure)
                {
                    // sentinel, show that we have a read failure and then yield the next valid object
                    yield return default;
                    pendingFailure = false;
                }

                yield return new COR_HEAPOBJECT
                {
                    address = reportedAddr.Value,
                    size = size,
                    type = new COR_TYPEID { token1 = mt.Value, token2 = 0 },
                };
            }
        }

        // Trailing corruption: if validation failed after the last successful yield (or
        // before any yield at all), show a sentinel now.
        if (pendingFailure)
            yield return default;
    }

    private IEnumerable<(GCHeapSegmentInfo Segment, GCHeapData Heap)> EnumerateAllSegments()
    {
        string[] gcIdentifiers = _gc.GetGCIdentifiers();
        bool isWorkstation = gcIdentifiers.Contains(GCIdentifiers.Workstation);
        foreach (GCHeapData heap in EnumerateHeaps(_gc, isWorkstation))
        {
            foreach (GCHeapSegmentInfo seg in _gc.EnumerateHeapSegments(heap))
            {
                yield return (seg, heap);
            }
        }
    }

    private bool TryGetObjectSize(TargetPointer objAddr, TargetPointer mt, out ulong size)
    {
        size = 0;
        try
        {
            TypeHandle handle = _rts.GetTypeHandle(mt);
            ulong baseSize = _rts.GetBaseSize(handle);
            uint componentSize = _rts.GetComponentSize(handle);
            uint numComponentsOffset = 0;
            if (componentSize != 0)
            {
                if (_rts.IsArray(handle, out _) || _rts.IsFreeObjectMethodTable(handle))
                    numComponentsOffset = _numComponentsOffsetArray;
                else if (_rts.IsString(handle))
                    numComponentsOffset = _numComponentsOffsetString;
                else
                    return false; // unrecognized component type
                if (!_target.TryRead(objAddr.Value + numComponentsOffset, out uint numComponents))
                    return false;
                baseSize += (ulong)componentSize * numComponents;
            }
            size = baseSize;
            return true;
        }
        catch
        {
            // The MT may be corrupt — surface as a read failure.
            return false;
        }
    }

    private static IEnumerable<GCHeapData> EnumerateHeaps(IGC gc, bool isWorkstation)
    {
        if (isWorkstation)
        {
            yield return gc.GetHeapData();
        }
        else
        {
            foreach (TargetPointer heapAddress in gc.GetGCHeaps())
                yield return gc.GetHeapData(heapAddress);
        }
    }
}
