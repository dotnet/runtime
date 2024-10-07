// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

// Given a contiguous region of memory in which we lay out a collection of non-overlapping code blocks that are
// not too small (so that two adjacent ones aren't too close together) and  where the start of each code block is preceeded by a code header aligned on some power of 2,
// we can break up the whole memory space into buckets of a fixed size (32-bytes in the current implementation), where
// each bucket either has a code block header or not.
// Thinking of each code block header address as a hex number, we can view it as: [index, offset, zeros]
// where each index gives us a bucket and the offset gives us the position of the header within the bucket.
// We encode each offset into a 4-bit nibble, reserving the special value 0 to mark the places in the map where a method doesn't start.
//
// To find the start of a method given an address we first convert it into a bucket index (giving the map unit)
// and an offset which we can then turn into the index of the nibble that covers that address.
// If the nibble is non-zero, we have the start of a method and it is near the given address.
// If the nibble is zero, we have to search backward first through the current map unit, and then through previous map
// units until we find a non-zero nibble.
internal sealed class NibbleMap
{
    // The contents of the map are 32-bit integers, where each nibble is a 4-bit integer.
    internal readonly struct MapUnit
    {
        public const int SizeInBytes = sizeof(uint);
        public const ulong SizeInNibbles = 2 * SizeInBytes;
        public readonly uint Value;

        public MapUnit(uint value) => Value = value;

        public override string ToString() => Value.ToString();

        public MapUnit LogicalShiftRight(int bits) => new MapUnit(Value >> bits);

        // Shift the next nibble into the least significant position.
        public MapUnit ShiftNextNibble => new MapUnit(Value >>> 4);

        public const uint NibbleMask = 0x0Fu;
        public uint Nibble => Value & NibbleMask;

        public bool IsZero => Value == 0;
    }

    public static NibbleMap Create(Target target)
    {
        return new NibbleMap(target);
    }

    private readonly Target _target;
    private NibbleMap(Target target)
    {
        _target = target;
    }



    // we will partition the address space into buckets of this many bytes.
    // There is at most one code block header per bucket.
    // Normally we would then need 5 bits (Log2(BytesPerBucket))to find the exact start address,
    // but because code headers are aligned, we can store the offset in a 4-bit nibble instead and shift appropriately to compute
    // the effective address
    private const ulong BytesPerBucket = 8 * MapUnit.SizeInBytes;


    // given the index of a nibble in the map, compute how much we have to shift a MapUnit to put that
    // nibble in the least significant position.
    internal static int ComputeNibbleShift(ulong mapIdx)
    {
        // the low bits of the nibble index give us how many nibbles we have to shift by.
        int nibbleOffsetInMapUnit = (int)(mapIdx & (MapUnit.SizeInNibbles - 1));
        return 28 - (nibbleOffsetInMapUnit * 4);  // bit shift - 4 bits per nibble
    }

    // Given a map index and a nibble index, compute the byte offset in the address space that they represent
    private static ulong ComputeByteOffset(ulong mapIdx, uint nibble)
    {
        return mapIdx * BytesPerBucket + (nibble - 1) * MapUnit.SizeInBytes;
    }

    // Given a base address, a map index, and a nibble index, compute the absolute address in memory
    //  that the index and nibble point to.
    private static TargetPointer GetAbsoluteAddress(TargetPointer baseAddress, ulong mapIdx, uint nibble)
    {
        return baseAddress + ComputeByteOffset(mapIdx, nibble);
    }

    // Given a relative address, decompose it into
    //  the bucket index and an offset within the bucket.
    private static void DecomposeAddress(TargetNUInt relative, out ulong mapIdx, out uint bucketByteIndex)
    {
        mapIdx = relative.Value / BytesPerBucket;
        bucketByteIndex = ((uint)(relative.Value & (BytesPerBucket - 1)) / MapUnit.SizeInBytes) + 1;
        System.Diagnostics.Debug.Assert(bucketByteIndex == (bucketByteIndex & MapUnit.NibbleMask));
    }

    // Given a logical index into the map, compute the address in memory where that map unit is located
    private static TargetPointer GetMapUnitAddress(TargetPointer mapStart, ulong mapIdx)
    {
        ulong bucketIndex = mapIdx / MapUnit.SizeInNibbles;
        return mapStart + bucketIndex * MapUnit.SizeInBytes;
    }

    internal static TargetPointer RoundTripAddress(TargetPointer mapBase, TargetPointer currentPC)
    {
        TargetNUInt relativeAddress = new TargetNUInt(currentPC.Value - mapBase.Value);
        DecomposeAddress(relativeAddress, out ulong mapIdx, out uint bucketByteIndex);
        return GetAbsoluteAddress(mapBase, mapIdx, bucketByteIndex);
    }

    private MapUnit ReadMapUnit(TargetPointer mapStart, ulong mapIdx)
    {
        return new MapUnit (_target.Read<uint>(GetMapUnitAddress(mapStart, mapIdx)));
    }

    internal TargetPointer FindMethodCode(TargetPointer mapBase, TargetPointer mapStart, TargetCodePointer currentPC)
    {
        TargetNUInt relativeAddress = new TargetNUInt(currentPC.Value - mapBase.Value);
        DecomposeAddress(relativeAddress, out ulong mapIdx, out uint bucketByteIndex);

        MapUnit t = ReadMapUnit(mapStart, mapIdx);

        // shift the nibble we want to the least significant position
        t = t.LogicalShiftRight(ComputeNibbleShift(mapIdx));
        uint nibble = t.Nibble;
        if (nibble != 0 && nibble <= bucketByteIndex)
        {
            return GetAbsoluteAddress(mapBase, mapIdx, nibble);
        }

        // search backwards through the current map unit
        // we processed the lsb nibble, move to the next one
        t = t.ShiftNextNibble;

        // if there's any nibble set in the current unit, find it
        if (!t.IsZero)
        {
            mapIdx--;
            nibble = t.Nibble;
            while (nibble == 0)
            {
                t = t.ShiftNextNibble;
                mapIdx--;
                nibble = t.Nibble;
            }
            return GetAbsoluteAddress(mapBase, mapIdx, nibble);
        }

        // if we were near the beginning of the address space, there is not enough space for the code header,
        // so we can stop
        if (mapIdx < MapUnit.SizeInNibbles)
        {
            return TargetPointer.Null;
        }

        // We're now done with the current map index.
        // Align the map index and move to the previous map unit, then move back one nibble.
#pragma warning disable IDE0054 // use compound assignment
        mapIdx = mapIdx & (~(MapUnit.SizeInNibbles - 1));
        mapIdx--;
#pragma warning restore IDE0054 // use compound assignment

        // read the map unit containing mapIdx and skip over it if it is all zeros
        while (mapIdx >= MapUnit.SizeInNibbles)
        {
            t = ReadMapUnit(mapStart, mapIdx);
            if (!t.IsZero)
                break;
            mapIdx -= MapUnit.SizeInNibbles;
        }

        // if we went all the way to the front, we didn't find a code header
        if (mapIdx < MapUnit.SizeInNibbles)
        {
            return TargetPointer.Null;
        }

        // move to the correct nibble in the map unit
        while (mapIdx != 0 && t.Nibble == 0)
        {
            t = t.ShiftNextNibble;
            mapIdx--;
        }

        if (mapIdx == 0 && t.IsZero)
        {
            return TargetPointer.Null;
        }

        nibble = t.Nibble;
        return GetAbsoluteAddress(mapBase, mapIdx, nibble);
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
