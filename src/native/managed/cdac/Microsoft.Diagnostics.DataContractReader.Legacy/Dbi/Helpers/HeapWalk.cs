// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal sealed class HeapWalk : IEnum<COR_HEAPOBJECT>
{
    private readonly IGC _gc;
    private readonly IObject _object;
    private readonly TargetPointer _freeObjectMT;
    private readonly Target _target;
    public IEnumerator<COR_HEAPOBJECT> Enumerator { get; }
    public nuint LegacyHandle { get; set; } = 0;

    public HeapWalk(Target target)
    {
        _gc = target.Contracts.GC;
        _object = target.Contracts.Object;
        _freeObjectMT = target.Contracts.RuntimeTypeSystem.GetWellKnownMethodTable(WellKnownMethodTable.Free);
        _target = target;
        Enumerator = Walk().GetEnumerator();
    }

    private IEnumerable<COR_HEAPOBJECT> Walk()
    {
        bool pendingFailure = false;

        foreach ((GCHeapSegmentInfo seg, GCHeapData _) in _gc.EnumerateAllSegments())
        {
            using IDisposable _ = _target.BeginCacheScope(new LinearReadCache(_target));
            if (seg.Start.Value >= seg.End.Value)
                continue;

            TargetPointer currentObj = _gc.GetPotentialNextObjectAddress(seg.Start, 0, seg);
            while (currentObj.Value < seg.End.Value)
            {
                TargetPointer mt;
                ulong size;
                try
                {
                    mt = _object.GetMethodTableAddress(currentObj);
                }
                catch
                {
                    pendingFailure = true;
                    break;
                }

                try
                {
                    size = _object.GetSize(currentObj);
                }
                catch
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
}
