// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

// Resolves an address that may point into the interior of a managed object to the address of the
// containing object by linearly walking the GC heap segment objects. This is used by GcScanContext
// to resolve interior stack roots and by RefWalk to resolve external memory handle roots.
public sealed class GCInteriorPointerResolver
{
    private readonly IGC _gc;
    private readonly IRuntimeTypeSystem _rts;

    private readonly LinearReadCache _cache;
    private readonly uint _numComponentsOffsetArray;
    private readonly uint _numComponentsOffsetString;
    private readonly ulong _methodTableOffset;
    private readonly byte _objectToMethodTableUnmask;

    public GCInteriorPointerResolver(Target target)
        : this(target, target.Contracts.GC, target.Contracts.RuntimeTypeSystem)
    {
    }

    internal GCInteriorPointerResolver(Target target, IGC gc, IRuntimeTypeSystem rts)
    {
        _gc = gc;
        _rts = rts;
        _cache = new LinearReadCache(target);
        _numComponentsOffsetArray = (uint)Data.Array.GetNumComponentsOffset(target);
        _numComponentsOffsetString = (uint)Data.String.GetStringLengthOffset(target);
        _methodTableOffset = (ulong)Data.Object.GetMethodTableOffset(target);
        _objectToMethodTableUnmask = target.ReadGlobal<byte>(Constants.Globals.ObjectToMethodTableUnmask);
    }

    // Resolves obj (an address that may point anywhere within a managed object) to the address of
    // the object that contains it, or TargetPointer.Null if obj does not fall within a live object
    // on any GC heap segment (including if obj is itself null/invalid, or the heap data is corrupt).
    public TargetPointer Resolve(TargetPointer obj)
    {
        TargetPointer outerObj = TargetPointer.Null;
        foreach ((GCHeapSegmentInfo seg, GCHeapData _) in _gc.EnumerateAllSegments())
        {
            if (obj.Value < seg.Start.Value || obj.Value >= seg.End.Value)
                continue;

            TargetPointer currentObj = _gc.GetPotentialNextObjectAddress(seg.Start, 0, seg);
            ulong size = 0;
            while (currentObj.Value <= obj.Value)
            {
                // Replicate IObject.GetMethodTableAddress in fast path with linear read cache
                if (!_cache.TryReadPointer(currentObj.Value + _methodTableOffset, out TargetPointer mt))
                {
                    return TargetPointer.Null;
                }
                mt = mt.Value & (ulong)~_objectToMethodTableUnmask;

                // Replicate IObject.GetSize in fast path with linear read cache
                if (!TryGetObjectSize(currentObj, mt, out size) || size == 0)
                {
                    return TargetPointer.Null;
                }

                size = _gc.AlignObjectSize(size, seg.Generation);
                if (currentObj.Value + size > seg.End.Value || size == 0)
                {
                    return TargetPointer.Null;
                }
                outerObj = currentObj;
                currentObj = _gc.GetPotentialNextObjectAddress(currentObj, size, seg);
            }
            return outerObj + size > obj ? outerObj : TargetPointer.Null;
        }
        return outerObj;
    }

    private bool TryGetObjectSize(TargetPointer objAddr, TargetPointer mt, out ulong size)
    {
        size = 0;
        try
        {
            ITypeHandle handle = _rts.GetTypeHandle(mt);
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
