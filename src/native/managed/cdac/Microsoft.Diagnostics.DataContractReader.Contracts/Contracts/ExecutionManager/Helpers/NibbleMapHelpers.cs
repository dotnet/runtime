// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Diagnostics;
using System;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

internal static class NibbleMapHelpers
{
    // we will partition the address space into buckets of this many bytes.
    // There is at most one code block header per bucket.
    // Normally we would then need 5 bits (Log2(BytesPerBucket))to find the exact start address,
    // but because code headers are aligned, we can store the offset in a 4-bit nibble instead and shift appropriately to compute
    // the effective address
    internal const ulong BytesPerBucket = 8 * MapUnit.SizeInBytes;

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
        internal Nibble Nibble => new(Value & NibbleMask);

        public bool IsEmpty => Value == 0;

        // Assuming mapIdx is the index of a nibble within the current map unit,
        // shift the unit so that nibble is in the least significant position and return the result.
        public MapUnit FocusOnIndexedNibble(MapKey mapIdx)
        {
            uint shift = mapIdx.GetNibbleShift();
            return new MapUnit(Value >>> (int)shift);
        }
    }

    // Each nibble is a 4-bit integer that gives an offset within a bucket.
    // We reserse 0 to mean that there is no method starting at any offset within a bucket
    internal readonly struct Nibble
    {
        public readonly uint Value;

        public Nibble(uint value)
        {
            Debug.Assert(value <= 0xF);
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
        private readonly ulong _mapIdx;
        public MapKey(ulong mapIdx) => _mapIdx = mapIdx;
        public override string ToString() => $"0x{_mapIdx:x}";

        // The offset of the address in the target space that this map index represents
        public ulong TargetByteOffset => _mapIdx * BytesPerBucket;

        // The index of the map unit that contains this map index
        public ulong ContainingMapUnitIndex => _mapIdx / MapUnit.SizeInNibbles;

        // The offset of the map unit that contains this map index
        public ulong ContainingMapUnitByteOffset => ContainingMapUnitIndex * MapUnit.SizeInBytes;

        // The map index is the index of a nibble within the map, this gives the index of that nibble within a map unit.
        public uint NibbleIndexInMapUnit => (uint)(_mapIdx & (MapUnit.SizeInNibbles - 1));

        // go to the previous nibble
        public MapKey Prev
        {
            get
            {
                Debug.Assert(_mapIdx > 0);
                return new MapKey(_mapIdx - 1);
            }

        }

        // to to the previous map unit
        public MapKey PrevMapUnit => new MapKey(_mapIdx - MapUnit.SizeInNibbles);

        // Get a MapKey that is aligned to the first nibble in the map unit that contains this map index
        public MapKey AlignDownToMapUnit() =>new MapKey(_mapIdx & (~(MapUnit.SizeInNibbles - 1)));

        // If the map index is less than the size of a map unit, we are in the first MapUnit and
        // can stop searching
        public bool InFirstMapUnit => _mapIdx < MapUnit.SizeInNibbles;

        public bool IsZero => _mapIdx == 0;

        // given the index of a nibble in the map, compute how much we have to shift a MapUnit to put that
        // nibble in the least significant position.
        internal uint GetNibbleShift()
        {
            return 28 - (NibbleIndexInMapUnit * 4);  // bit shift - 4 bits per nibble
        }

        internal MapUnit ReadMapUnit(Target target, TargetPointer mapStart)
        {
            // Given a logical index into the map, compute the address in memory where that map unit is located
            TargetPointer mapUnitAdderss = mapStart + ContainingMapUnitByteOffset;
            return new MapUnit(target.Read<uint>(mapUnitAdderss));
        }
    }

    // for tests
    internal static uint ComputeNibbleShift(MapKey mapIdx) => mapIdx.GetNibbleShift();

    internal static TargetPointer RoundTripAddress(TargetPointer mapBase, TargetPointer currentPC)
    {
        TargetNUInt relativeAddress = new TargetNUInt(currentPC.Value - mapBase.Value);
        DecomposeAddress(relativeAddress, out MapKey mapIdx, out Nibble bucketByteIndex);
        return GetAbsoluteAddress(mapBase, mapIdx, bucketByteIndex);
    }

    // Given a base address, a map index, and a nibble value, compute the absolute address in memory
    // that the index and nibble point to.
    internal static TargetPointer GetAbsoluteAddress(TargetPointer baseAddress, MapKey mapIdx, Nibble nibble)
    {
        return baseAddress + mapIdx.TargetByteOffset + nibble.TargetByteOffset;
    }

    // Given a relative address, decompose it into
    // the bucket index and an offset within the bucket.
    internal static void DecomposeAddress(TargetNUInt relative, out MapKey mapIdx, out Nibble bucketByteIndex)
    {
        mapIdx = new(relative.Value / BytesPerBucket);
        uint bucketByteOffset = (uint)(relative.Value & (BytesPerBucket - 1));
        bucketByteIndex = new Nibble((bucketByteOffset / MapUnit.SizeInBytes) + 1);
    }
}
