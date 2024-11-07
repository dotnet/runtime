// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Diagnostics;
using System;

using static Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers.NibbleMapHelpers;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

// CoreCLR nibblemap with O(1) lookup time.

internal class NibbleMap_2 : INibbleMap
{
    private readonly Target _target;

    private NibbleMap_2(Target target)
    {
        _target = target;
    }

    internal static bool IsPointer(MapUnit mapUnit)
    {
        return (mapUnit.Value & MapUnit.NibbleMask) > 8;
    }

    internal static TargetPointer DecodePointer(TargetPointer baseAddress, MapUnit mapUnit)
    {
        uint nibble = mapUnit.Value & MapUnit.NibbleMask;
        uint relativePointer = (mapUnit.Value & ~MapUnit.NibbleMask) + ((nibble - 9) << 2);
        return baseAddress + relativePointer;
    }

    internal static uint EncodePointer(uint relativeAddress)
    {
        uint nibble = ((relativeAddress & MapUnit.NibbleMask) >>> 2) + 9;
        return (relativeAddress & ~MapUnit.NibbleMask) + nibble;
    }

    internal TargetPointer FindMethodCode(TargetPointer mapBase, TargetPointer mapStart, TargetCodePointer currentPC)
    {
        TargetNUInt relativeAddress = new TargetNUInt(currentPC.Value - mapBase.Value);
        DecomposeAddress(relativeAddress, out MapKey mapIdx, out Nibble bucketByteIndex);

        MapUnit t = mapIdx.ReadMapUnit(_target, mapStart);

        // if pointer, return value
        if (IsPointer(t))
        {
            return DecodePointer(mapBase, t);
        }

        // shift the nibble we want to the least significant position
        t = t.FocusOnIndexedNibble(mapIdx);

        // if the nibble is non-zero, we have found the start of a method,
        // but we need to check that the start is before the current address, not after
        if (!t.Nibble.IsEmpty && t.Nibble.Value <= bucketByteIndex.Value)
        {
            return GetAbsoluteAddress(mapBase, mapIdx, t.Nibble);
        }

        // search backwards through the current map unit
        // we processed the lsb nibble, move to the next one
        t = t.ShiftNextNibble;

        // if there's any nibble set in the current unit, find it
        if (!t.IsZero)
        {
            mapIdx = mapIdx.Prev;
            while (t.Nibble.IsEmpty)
            {
                t = t.ShiftNextNibble;
                mapIdx = mapIdx.Prev;
            }
            return GetAbsoluteAddress(mapBase, mapIdx, t.Nibble);
        }

        // We finished the current map unit, we want to move to the previous one.
        // But if we were in the first map unit, we can stop
        if (mapIdx.InFirstMapUnit)
        {
            return TargetPointer.Null;
        }

        // We're now done with the current map unit.
        // Align the map index to the current map unit, then move back one nibble into the previous map unit
        mapIdx = mapIdx.AlignDownToMapUnit();
        mapIdx = mapIdx.Prev;

        // read the map unit containing mapIdx and skip over it if it is all zeros
        t = mapIdx.ReadMapUnit(_target, mapStart);

        // if t is not zero, we either have a pointer or a nibble
        if (!t.IsZero)
        {
            if (IsPointer(t))
            {
                return DecodePointer(mapBase, t);
            }

            // move to the correct nibble in the map unit
            while (!mapIdx.IsZero && t.Nibble.IsEmpty)
            {
                t = t.ShiftNextNibble;
                mapIdx = mapIdx.Prev;
            }

            return GetAbsoluteAddress(mapBase, mapIdx, t.Nibble);
        }

        return TargetPointer.Null;
    }

    public static INibbleMap Create(Target target)
    {
        return new NibbleMap_2(target);
    }

    public TargetPointer FindMethodCode(Data.CodeHeapListNode heapListNode, TargetCodePointer jittedCodeAddress)
    {
        if (jittedCodeAddress < heapListNode.StartAddress || jittedCodeAddress > heapListNode.EndAddress)
        {
            return TargetPointer.Null;
        }

        return FindMethodCode(heapListNode.MapBase, heapListNode.HeaderMap, jittedCodeAddress);
    }
}
