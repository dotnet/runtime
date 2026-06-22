// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal sealed class HeapWalk : IEnum<COR_HEAPOBJECT>
{
    private readonly IGC _gc;
    private readonly IObject _object;
    private readonly IRuntimeTypeSystem _rts;
    private readonly TargetPointer _freeObjectMT;
    private readonly LinearReadCache _cache;
    private readonly uint _numComponentsOffsetArray;
    private readonly uint _numComponentsOffsetString;
    private readonly uint _methodTableOffset;
    private readonly ulong _methodTableMask;

    public IEnumerator<COR_HEAPOBJECT> Enumerator { get; }
    public nuint LegacyHandle { get; set; } = 0;

    public HeapWalk(Target target)
    {
        _gc = target.Contracts.GC;
        _object = target.Contracts.Object;
        _rts = target.Contracts.RuntimeTypeSystem;
        _freeObjectMT = _rts.GetWellKnownMethodTable(WellKnownMethodTable.Free);
        _cache = new LinearReadCache(target);
        // use these fields directly instead of through RuntimeTypeSystem so that we can use our cache that we really only need for heap walking
        _numComponentsOffsetArray = (uint)target.GetTypeInfo(DataType.Array).Fields[Constants.FieldNames.Array.NumComponents].Offset;
        _numComponentsOffsetString = (uint)target.GetTypeInfo(DataType.String).Fields["m_StringLength"].Offset;
        _methodTableOffset = (uint)target.GetTypeInfo(DataType.Object).Fields["m_pMethTab"].Offset;
        _methodTableMask = (ulong)~target.ReadGlobal<byte>(Constants.Globals.ObjectToMethodTableUnmask);

        Enumerator = Walk().GetEnumerator();
    }

    private IEnumerable<COR_HEAPOBJECT> Walk()
    {
        bool pendingFailure = false;

        foreach ((GCHeapSegmentInfo seg, GCHeapData _) in _gc.EnumerateAllSegments())
        {
            if (seg.Start.Value >= seg.End.Value)
                continue;

            TargetPointer currentObj = _gc.GetPotentialNextObjectAddress(seg.Start, 0, seg);
            while (currentObj.Value < seg.End.Value)
            {
                // Replicate IObject.GetMethodTableAddress in fast path with linear read cache
                if (!_cache.TryReadPointer(currentObj.Value + _methodTableOffset, out TargetPointer mt))
                {
                    pendingFailure = true;
                    break;
                }
                mt = new TargetPointer(mt.Value & _methodTableMask);

                // Replicate IObject.GetSize in fast path with linear read cache
                if (!TryGetObjectSize(currentObj, mt, out ulong size) || size == 0)
                {
                    pendingFailure = true;
                    break;
                }

                size = _gc.AlignObjectSize(size, seg.Generation);
                if (currentObj.Value + size > seg.End.Value || size == 0)
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
                if (!_cache.TryReadUInt32(objAddr.Value + numComponentsOffset, out uint numComponents))
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
}
