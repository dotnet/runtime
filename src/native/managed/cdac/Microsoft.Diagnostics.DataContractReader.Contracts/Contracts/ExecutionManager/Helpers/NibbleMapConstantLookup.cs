// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Diagnostics;
using System;

using static Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers.NibbleMapHelpers;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

// CoreCLR nibblemap with O(1) lookup time.
//
// Implementation very similar to NibbleMapLinearLookup, but with the addition of writing relative pointers
// into the nibblemap whenever a code block completely covers a DWORD. This allows for O(1) lookup
// with the cost of O(n) write time.
//
// Pointers are encoded using the top 28 bits of the DWORD normally, the bottom 4 bits of the pointer
// are reduced to 2 bits due to 4 byte code offset and encoded in bits 28 .. 31 of the DWORD with values
// 9-12. This is used to differentiate nibble values and pointer DWORDs.
//
// To read the nibblemap, we check if the DWORD is a pointer. If so, then we know the value currentPC is
// part of a managed code block beginning at the mapBase + decoded pointer. If the DWORD is empty
// (no pointer or previous nibbles), then we only need to read the previous DWORD. If that DWORD is empty,
// then we must not be in a managed function. Otherwise the write algorithm would have written a relative
// pointer in the DWORD.
//
// Note, a currentPC pointing to bytes outside a function have undefined lookup behavior.
// In this implementation we may "extend" the lookup period of a function several hundred bytes
// if there is not another function following it.

internal sealed class NibbleMapConstantLookup : INibbleMap
{
    private readonly Target _target;

    private NibbleMapConstantLookup(Target target)
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
        if (!t.IsEmpty)
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

        // if t is empty, then currentPC can not be in a function
        if (t.IsEmpty)
        {
            return TargetPointer.Null;
        }

        // if t is not empty, it must contain a pointer or a nibble
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

    public static INibbleMap Create(Target target)
    {
        return new NibbleMapConstantLookup(target);
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
