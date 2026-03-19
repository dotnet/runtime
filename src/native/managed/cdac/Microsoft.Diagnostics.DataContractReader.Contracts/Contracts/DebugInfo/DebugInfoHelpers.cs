// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.Reflection.ReadyToRun;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Shared bounds decoding helpers for DebugInfo contract versions.
/// V1: 2-bit enumerated source type.
/// V2: 3-bit flags (CallInstruction, StackEmpty, Async).
/// </summary>
internal static class DebugInfoHelpers
{
    private const uint IL_OFFSET_BIAS = unchecked((uint)-3);
    private const uint SOURCE_TYPE_BITS_V1 = 2;
    private const uint SOURCE_TYPE_BITS_V2 = 3;


    internal static IEnumerable<OffsetMapping> DoBounds(NativeReader nativeReader, uint version)
    {
        NibbleReader reader = new(nativeReader, 0);

        uint boundsEntryCount = reader.ReadUInt();
        Debug.Assert(boundsEntryCount > 0, "Expected at least one entry in bounds.");

        uint bitsForNativeDelta = reader.ReadUInt() + 1; // Number of bits needed for native deltas
        uint bitsForILOffsets = reader.ReadUInt() + 1; // Number of bits needed for IL offsets

        uint sourceTypeBitCount = (version == 1) ? SOURCE_TYPE_BITS_V1 : SOURCE_TYPE_BITS_V2;
        uint bitsPerEntry = bitsForNativeDelta + bitsForILOffsets + sourceTypeBitCount;
        ulong bitsMeaningfulMask = (1UL << ((int)bitsPerEntry)) - 1;
        int offsetOfActualBoundsData = reader.GetNextByteOffset();

        uint bitsCollected = 0;
        ulong bitTemp = 0;
        uint curBoundsProcessed = 0;

        uint previousNativeOffset = 0;

        while (curBoundsProcessed < boundsEntryCount)
        {
            bitTemp |= ((uint)nativeReader[offsetOfActualBoundsData++]) << (int)bitsCollected;
            bitsCollected += 8;
            while (bitsCollected >= bitsPerEntry)
            {
                ulong mappingDataEncoded = bitsMeaningfulMask & bitTemp;
                bitTemp >>= (int)bitsPerEntry;
                bitsCollected -= bitsPerEntry;

                ulong sourceTypeBitsMask = (1UL << ((int)sourceTypeBitCount)) - 1;
                ulong sourceTypeBits = mappingDataEncoded & sourceTypeBitsMask;
                SourceTypes sourceType = 0;
                if ((sourceTypeBits & 0x1) != 0) sourceType |= SourceTypes.CallInstruction;
                if ((sourceTypeBits & 0x2) != 0) sourceType |= SourceTypes.StackEmpty;
                if ((sourceTypeBits & 0x4) != 0) sourceType |= SourceTypes.Async;

                mappingDataEncoded >>= (int)sourceTypeBitCount;
                uint nativeOffsetDelta = (uint)(mappingDataEncoded & ((1UL << (int)bitsForNativeDelta) - 1));
                previousNativeOffset += nativeOffsetDelta;
                uint nativeOffset = previousNativeOffset;

                mappingDataEncoded >>= (int)bitsForNativeDelta;
                uint ilOffset = (uint)mappingDataEncoded + IL_OFFSET_BIAS;

                yield return new OffsetMapping()
                {
                    NativeOffset = nativeOffset,
                    ILOffset = ilOffset,
                    SourceType = sourceType
                };
                curBoundsProcessed++;
            }
        }
    }
}
