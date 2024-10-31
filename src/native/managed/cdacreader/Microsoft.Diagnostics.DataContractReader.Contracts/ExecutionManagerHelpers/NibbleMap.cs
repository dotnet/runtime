// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Diagnostics;
using System;

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
//
// For example (all code addresses are relative to some unspecified base):
//  Suppose there is code starting at address 304 (0x130)
//  Then the map index will be 304 / 32 = 9 and the byte offset will be 304 % 32 = 16
//  Because addresses are 4-byte aligned, the nibble value will be 1 + 16 / 4 = 5  (we reserve 0 to mean no method).
//  So the map unit containing index 9 will contain the value 0x5 << 24 (the map index 9 means we want the second nibble in the second map unit, and we number the nibbles starting from the most significant)
//  Or 0x05000000
//
//  Now suppose we do a lookup for address 306 (0x132)
//  The map index will be 306 / 32 = 9 and the byte offset will be 306 % 32 = 18
//  The nibble value will be 1 + 18 / 4 = 5
//  To do the lookup, we will load the map unit with index 9 (so the second 32-bit unit in the map) and get the value 0x05000000
//  We will then shift to focus on the nibble with map index 9 (which again has nibble shift 24), so
//  the map unit will be 0x00000005 and we will get the nibble value 5.
//  Therefore we know that there is a method start at map index 9, nibble value 5.
//  The map index corresponds to an offset of 288 bytes and the nibble value 5 corresponds to an offset of (5 - 1) * 4 = 16 bytes
//  So the method starts at offset 288 + 16 = 304, which is the address we were looking for.
//
//  Now suppose we do a lookup for address 302 (0x12E)
//  The map index will be 302 / 32 = 9 and the byte offset will be 302 % 32 = 14
//  The nibble value will be 1 + 14 / 4 = 4
//  To do the lookup, we will load the map unit containing map index 9 and get the value 0x05000000
//  We will then shift to focus on the nibble with map index 9 (which again has nibble shift 24), so we will get
//  the nibble value 5.
//  Therefore we know that there is a method start at map index 9, nibble value 5.
//  But the address we're looking for is map index 9, nibble value 4.
//  We know that methods can't start within 32-bytes of each other, so we know that the method we're looking for is not in the current nibble.
//  We will then try to shift to the previous nibble in the map unit (0x00000005 >> 4 = 0x00000000)
//  Therefore we know there is no method start at any map index in the current map unit.
//  We will then align the map index to the start of the current map unit (map index 8) and move back to the previous map unit (map index 7)
//  At that point, we scan backwards for non-zero map units. Since there are none, we return null.

internal class NibbleMap
{
    // We load the map contents as 32-bit integers, which contains 8 4-bit nibbles.
    // The algorithm will focus on each nibble in a map unit before moving on to the previous map unit
    internal readonly struct MapUnit
    {
        public const int SizeInBytes = sizeof(uint);
        public const ulong SizeInNibbles = 2 * SizeInBytes;
        public readonly uint Value;

        public MapUnit(uint value) => Value = value;

        public override string ToString() => $"0x{Value:x}";

        // Shift the next nibble into the least significant position.
        public MapUnit ShiftNextNibble => new MapUnit(Value >>> 4);

        public const uint NibbleMask = 0x0Fu;
        internal Nibble Nibble => new ((int)(Value & NibbleMask));

        public bool IsZero => Value == 0;

        // Assuming mapIdx is the index of a nibble within the current map unit,
        // shift the unit so that nibble is in the least significant position and return the result.
        public MapUnit FocusOnIndexedNibble(MapKey mapIdx)
        {
            int shift = mapIdx.GetNibbleShift();
            return new MapUnit (Value >>> shift);
        }
    }

    // Each nibble is a 4-bit integer that gives an offset within a bucket.
    // We reserse 0 to mean that there is no method starting at any offset within a bucket
    internal readonly struct Nibble
    {
        public readonly int Value;

        public Nibble(int value)
        {
            Debug.Assert (value >= 0 && value <= 0xF);
            Value = value;
        }

        public static Nibble Zero => new Nibble(0);
        public bool IsEmpty => Value == 0;

        public ulong TargetByteOffset
        {
            get
            {
                Debug.Assert(Value != 0);
                return (uint)(Value - 1) * MapUnit.SizeInBytes;
            }
        }
    }

    // The key to the map is the index of an individual nibble
    internal readonly struct MapKey
    {
        private readonly ulong MapIdx;
        public MapKey(ulong mapIdx) => MapIdx = mapIdx;
        public override string ToString() => $"0x{MapIdx:x}";

        // The offset of the address in the target space that this map index represents
        public ulong TargetByteOffset => MapIdx * BytesPerBucket;

        // The index of the map unit that contains this map index
        public ulong ContainingMapUnitIndex => MapIdx / MapUnit.SizeInNibbles;

        // The offset of the map unit that contains this map index
        public ulong ContainingMapUnitByteOffset => ContainingMapUnitIndex * MapUnit.SizeInBytes;

        // The map index is the index of a nibble within the map, this gives the index of that nibble within a map unit.
        public int NibbleIndexInMapUnit => (int)(MapIdx & (MapUnit.SizeInNibbles - 1));

        // go to the previous nibble
        public MapKey Prev => new MapKey(MapIdx - 1);

        // to to the previous map unit
        public MapKey PrevMapUnit => new MapKey(MapIdx - MapUnit.SizeInNibbles);

        // Get a MapKey that is aligned to the first nibble in the map unit that contains this map index
        public MapKey AlignDownToMapUnit() =>new MapKey(MapIdx & (~(MapUnit.SizeInNibbles - 1)));

        // If the map index is less than the size of a map unit, we are in the first MapUnit and
        // can stop searching
        public bool InFirstMapUnit => MapIdx < MapUnit.SizeInNibbles;

        public bool IsZero => MapIdx == 0;

        // given the index of a nibble in the map, compute how much we have to shift a MapUnit to put that
        // nibble in the least significant position.
        internal int GetNibbleShift()
        {
            return 28 - (NibbleIndexInMapUnit * 4);  // bit shift - 4 bits per nibble
        }
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


    // for tests
    internal static int ComputeNibbleShift(MapKey mapIdx) => mapIdx.GetNibbleShift();

    // Given a base address, a map index, and a nibble value, compute the absolute address in memory
    //  that the index and nibble point to.
    private static TargetPointer GetAbsoluteAddress(TargetPointer baseAddress, MapKey mapIdx, Nibble nibble)
    {
        return baseAddress + mapIdx.TargetByteOffset + nibble.TargetByteOffset;
    }

    // Given a relative address, decompose it into
    //  the bucket index and an offset within the bucket.
    private static void DecomposeAddress(TargetNUInt relative, out MapKey mapIdx, out Nibble bucketByteIndex)
    {
        mapIdx = new (relative.Value / BytesPerBucket);
        int bucketByteOffset = (int)(relative.Value & (BytesPerBucket - 1));
        bucketByteIndex = new Nibble ((bucketByteOffset / MapUnit.SizeInBytes) + 1);
    }

    internal static TargetPointer RoundTripAddress(TargetPointer mapBase, TargetPointer currentPC)
    {
        TargetNUInt relativeAddress = new TargetNUInt(currentPC.Value - mapBase.Value);
        DecomposeAddress(relativeAddress, out MapKey mapIdx, out Nibble bucketByteIndex);
        return GetAbsoluteAddress(mapBase, mapIdx, bucketByteIndex);
    }

    private MapUnit ReadMapUnit(TargetPointer mapStart, MapKey mapIdx)
    {
        // Given a logical index into the map, compute the address in memory where that map unit is located
        TargetPointer mapUnitAdderss = mapStart + mapIdx.ContainingMapUnitByteOffset;
        return new MapUnit (_target.Read<uint>(mapUnitAdderss));
    }

    internal TargetPointer FindMethodCode(TargetPointer mapBase, TargetPointer mapStart, TargetCodePointer currentPC)
    {
        TargetNUInt relativeAddress = new TargetNUInt(currentPC.Value - mapBase.Value);
        DecomposeAddress(relativeAddress, out MapKey mapIdx, out Nibble bucketByteIndex);

        MapUnit t = ReadMapUnit(mapStart, mapIdx);

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
        while (true)
        {
            t = ReadMapUnit(mapStart, mapIdx);
            if (!t.IsZero)
                break;
            if (mapIdx.InFirstMapUnit)
            {
                // we're at the first map unit and all the bits in the map unit are zero,
                // there is no code header to find
                return TargetPointer.Null;
            }
            mapIdx = mapIdx.PrevMapUnit;
        }

        Debug.Assert(!t.IsZero);

        // move to the correct nibble in the map unit
        while (!mapIdx.IsZero && t.Nibble.IsEmpty)
        {
            t = t.ShiftNextNibble;
            mapIdx = mapIdx.Prev;
        }

        if (mapIdx.IsZero && t.IsZero)
        {
            return TargetPointer.Null;
        }

        return GetAbsoluteAddress(mapBase, mapIdx, t.Nibble);
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
