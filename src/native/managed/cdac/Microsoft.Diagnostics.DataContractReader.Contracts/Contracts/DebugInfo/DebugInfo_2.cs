// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ILCompiler.Reflection.ReadyToRun;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class DebugInfo_2(Target target) : IDebugInfo
{
    private const uint DEBUG_INFO_FAT = 0;
    private const uint IL_OFFSET_BIAS = unchecked((uint)-3);

    private record struct DebugInfoChunks
    {
        public TargetPointer BoundsStart;
        public uint BoundsSize;
        public TargetPointer VarsStart;
        public uint VarsSize;
        public TargetPointer UninstrumentedBoundsStart;
        public uint UninstrumentedBoundsSize;
        public TargetPointer PatchpointInfoStart;
        public uint PatchpointInfoSize;
        public TargetPointer RichDebugInfoStart;
        public uint RichDebugInfoSize;
        public TargetPointer AsyncInfoStart;
        public uint AsyncInfoSize;
        public TargetPointer DebugInfoEnd;
    }

    private readonly Target _target = target;
    private readonly IExecutionManager _eman = target.Contracts.ExecutionManager;

    IEnumerable<OffsetMapping> IDebugInfo.GetMethodNativeMap(TargetCodePointer pCode, bool preferUninstrumented, out uint codeOffset)
    {
        // Get the method's DebugInfo
        if (_eman.GetCodeBlockHandle(pCode) is not CodeBlockHandle cbh)
            throw new InvalidOperationException($"No CodeBlockHandle found for native code {pCode}.");
        TargetPointer debugInfo = _eman.GetDebugInfo(cbh, out bool _);

        TargetCodePointer nativeCodeStart = _eman.GetStartAddress(cbh);
        codeOffset = (uint)(CodePointerUtils.AddressFromCodePointer(pCode, _target) - CodePointerUtils.AddressFromCodePointer(nativeCodeStart, _target));

        return RestoreBoundaries(debugInfo, preferUninstrumented);
    }

    private DebugInfoChunks DecodeChunks(TargetPointer debugInfo)
    {
        NativeReader nibbleNativeReader = new(new TargetStream(_target, debugInfo, 42 /*maximum size of 7 32bit ints compressed*/), _target.IsLittleEndian);
        NibbleReader nibbleReader = new(nibbleNativeReader, 0);

        uint countBoundsOrFatMarker = nibbleReader.ReadUInt();

        DebugInfoChunks chunks = default;

        if (countBoundsOrFatMarker == DEBUG_INFO_FAT)
        {
            // Fat header
            chunks.BoundsSize = nibbleReader.ReadUInt();
            chunks.VarsSize = nibbleReader.ReadUInt();
            chunks.UninstrumentedBoundsSize = nibbleReader.ReadUInt();
            chunks.PatchpointInfoSize = nibbleReader.ReadUInt();
            chunks.RichDebugInfoSize = nibbleReader.ReadUInt();
            chunks.AsyncInfoSize = nibbleReader.ReadUInt();
        }
        else
        {
            chunks.BoundsSize = countBoundsOrFatMarker;
            chunks.VarsSize = nibbleReader.ReadUInt();
            chunks.UninstrumentedBoundsSize = 0;
            chunks.PatchpointInfoSize = 0;
            chunks.RichDebugInfoSize = 0;
            chunks.AsyncInfoSize = 0;
        }

        chunks.BoundsStart = debugInfo + (uint)nibbleReader.GetNextByteOffset();
        chunks.VarsStart = chunks.BoundsStart + chunks.BoundsSize;
        chunks.UninstrumentedBoundsStart = chunks.VarsStart + chunks.VarsSize;
        chunks.PatchpointInfoStart = chunks.UninstrumentedBoundsStart + chunks.UninstrumentedBoundsSize;
        chunks.RichDebugInfoStart = chunks.PatchpointInfoStart + chunks.PatchpointInfoSize;
        chunks.AsyncInfoStart = chunks.RichDebugInfoStart + chunks.RichDebugInfoSize;
        chunks.DebugInfoEnd = chunks.AsyncInfoStart + chunks.AsyncInfoSize;
        return chunks;
    }

    private IEnumerable<OffsetMapping> RestoreBoundaries(TargetPointer debugInfo, bool preferUninstrumented)
    {
        DebugInfoChunks chunks = DecodeChunks(debugInfo);

        TargetPointer addrBounds = chunks.BoundsStart;
        uint cbBounds = chunks.BoundsSize;

        if (preferUninstrumented && chunks.UninstrumentedBoundsSize != 0)
        {
            // If we have uninstrumented bounds, we will use them instead of the regular bounds.
            addrBounds = chunks.UninstrumentedBoundsStart;
            cbBounds = chunks.UninstrumentedBoundsSize;
        }

        if (cbBounds > 0)
        {
            NativeReader boundsNativeReader = new(new TargetStream(_target, addrBounds, cbBounds), _target.IsLittleEndian);
            return DoBounds(boundsNativeReader);
        }

        return Enumerable.Empty<OffsetMapping>();
    }
    private static IEnumerable<OffsetMapping> DoBounds(NativeReader nativeReader)
    {
        NibbleReader reader = new(nativeReader, 0);

        uint boundsEntryCount = reader.ReadUInt();
        Debug.Assert(boundsEntryCount > 0, "Expected at least one entry in bounds.");

        uint bitsForNativeDelta = reader.ReadUInt() + 1; // Number of bits needed for native deltas
        uint bitsForILOffsets = reader.ReadUInt() + 1; // Number of bits needed for IL offsets

        uint bitsPerEntry = bitsForNativeDelta + bitsForILOffsets + 3; // 3 bits for source type
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

                SourceTypes sourceType = 0;

                if ((mappingDataEncoded & 0x1) != 0)
                    sourceType |= SourceTypes.CallInstruction;

                if ((mappingDataEncoded & 0x2) != 0)
                    sourceType |= SourceTypes.StackEmpty;

                if ((mappingDataEncoded & 0x4) != 0)
                    sourceType |= SourceTypes.Async;

                mappingDataEncoded >>= 2;
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
