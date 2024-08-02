// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;

using MapUnit = uint;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;


#pragma warning disable SA1121 // Use built in alias
internal readonly partial struct NativeCodePointers_1 : INativeCodePointers
{
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

        public static NibbleMap Create(Target target, uint codeHeaderSize)
        {
            return new NibbleMap(target, codeHeaderSize);
        }

        private readonly Target _target;
        private readonly uint _codeHeaderSize;
        private NibbleMap(Target target, uint codeHeaderSize)
        {
            _target = target;
            _codeHeaderSize = codeHeaderSize;
        }

        // Shift the next nibble into the least significant position.
        private static T NextNibble<T>(T n) where T : IBinaryInteger<T>
        {
            return n >>> 4;
        }


        private const uint MapUnitBytes = sizeof(MapUnit); // our units are going to be 32-bit integers
        private const MapUnit NibbleMask = 0x0F;
        private const ulong NibblesPerMapUnit = 2 * MapUnitBytes; // 2 nibbles per byte * N bytes per map unit

        // we will partition the address space into buckets of this many bytes.
        // There is at most one code block header per bucket.
        // normally we would then need Log2(BytesPerBucket) bits to find the exact start address,
        // but because code headers are aligned, we can store the offset in a 4-bit nibble instead and shift appropriately to compute
        // the effective address
        private const ulong BytesPerBucket = 8 * sizeof(MapUnit);


        // given the index of a nibble in the map, compute how much we have to shift a MapUnit to put that
        // nible in the least significant position.
        private static int ComputeNibbleShift(ulong mapIdx)
        {
            // the low bits of the nibble index give us how many nibbles we have to shift by.
            int nibbleOffsetInMapUnit = (int)(mapIdx & (NibblesPerMapUnit - 1));
            return 28 - (nibbleOffsetInMapUnit * 4);  // bit shift - 4 bits per nibble
        }

        private static ulong ComputeByteOffset(ulong mapIdx, uint nibble)
        {
            return mapIdx * BytesPerBucket + (nibble - 1) * MapUnitBytes;
        }
        private TargetPointer GetAbsoluteAddress(TargetPointer baseAddress, ulong mapIdx, uint nibble)
        {
            return baseAddress + ComputeByteOffset(mapIdx, nibble) - _codeHeaderSize;
        }

        // Given a relative address, decompose it into
        //  the bucket index and an offset within the bucket.
        private static void DecomposeAddress(TargetNUInt relative, out ulong mapIdx, out uint bucketByteIndex)
        {
            mapIdx = relative.Value / BytesPerBucket;
            bucketByteIndex = ((uint)(relative.Value & (BytesPerBucket - 1)) / MapUnitBytes) + 1;
            System.Diagnostics.Debug.Assert(bucketByteIndex == (bucketByteIndex & NibbleMask));
        }

        private static TargetPointer GetMapUnitAddress(TargetPointer mapStart, ulong mapIdx)
        {
            return mapStart + (mapIdx / NibblesPerMapUnit) * MapUnitBytes;
        }

        public TargetPointer FindMethodCode(TargetPointer mapBase, TargetPointer mapStart, TargetCodePointer currentPC)
        {
            TargetNUInt relativeAddress = new TargetNUInt(currentPC.Value - mapBase.Value);
            DecomposeAddress(relativeAddress, out ulong mapIdx, out uint bucketByteIndex);

            MapUnit t = _target.Read<MapUnit>(GetMapUnitAddress(mapStart, mapIdx));

            // shift the nibble we want to the least significant position
            t = t >>> ComputeNibbleShift(mapIdx);
            uint nibble = t & NibbleMask;
            if (nibble != 0 && nibble <= bucketByteIndex)
            {
                return GetAbsoluteAddress(mapBase, mapIdx, nibble);
            }

            // search backwards through the current map unit
            // we processed the lsb nibble, move to the next one
            t = NextNibble(t);

            // if there's any nibble set in the current unit, find it
            if (t != 0)
            {
                mapIdx--;
                nibble = t & NibbleMask;
                while (nibble == 0)
                {
                    t = NextNibble(t);
                    mapIdx--;
                    nibble = t & NibbleMask;
                }
                return GetAbsoluteAddress(mapBase, mapIdx, nibble);
            }

            // if we were near the beginning of the address space, there is not enough space for the code header,
            // so we can stop
            if (mapIdx < NibblesPerMapUnit)
            {
                return TargetPointer.Null;
            }

            // We're now done with the current map index.
            // Align the map index and move to the previous map unit, then move back one nibble.
#pragma warning disable IDE0054 // use compound assignment
            mapIdx = mapIdx & (~(NibblesPerMapUnit - 1)) - 1;
#pragma warning restore IDE0054 // use compound assignment

            // read the map unit containing mapIdx and skip over it if it is all zeros
            while (mapIdx >= NibblesPerMapUnit)
            {
                t = _target.Read<MapUnit>(GetMapUnitAddress(mapStart, mapIdx));
                if (t != 0)
                    break;
                mapIdx -= NibblesPerMapUnit;
            }

            // if we went all the way to the front, we didn't find a code header
            if (mapIdx < NibblesPerMapUnit)
            {
                return TargetPointer.Null;
            }

            // move to the correct nibble in the map unit
            while (mapIdx != 0 && (t & NibbleMask) == 0)
            {
                t = NextNibble(t);
                mapIdx--;
            }

            if (mapIdx == 0 && t == 0)
            {
                return TargetPointer.Null;
            }

            nibble = t & NibbleMask;
            return GetAbsoluteAddress(mapBase, mapIdx, nibble);
        }

    }
}
#pragma warning restore SA1121 // Use built in alias
