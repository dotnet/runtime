// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using ILCompiler.Reflection.ReadyToRun;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class DebugInfo_2(Target target) : IDebugInfo
{
    private const uint DEBUG_INFO_FAT = 0;

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
            return DebugInfoHelpers.DoBounds(boundsNativeReader, 2);
        }

        return [];
    }
}
