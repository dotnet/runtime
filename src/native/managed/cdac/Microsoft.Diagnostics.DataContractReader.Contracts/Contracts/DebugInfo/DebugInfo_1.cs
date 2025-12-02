// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.Reflection.ReadyToRun;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class DebugInfo_1(Target target) : IDebugInfo
{
    private const uint DEBUG_INFO_BOUNDS_HAS_INSTRUMENTED_BOUNDS = 0xFFFFFFFF;

    [Flags]
    internal enum ExtraDebugInfoFlags_1 : byte
    {
        // Debug info contains patchpoint information
        EXTRA_DEBUG_INFO_PATCHPOINT = 0x01,
        // Debug info contains rich information
        EXTRA_DEBUG_INFO_RICH = 0x02,
    }

    private readonly Target _target = target;
    private readonly IExecutionManager _eman = target.Contracts.ExecutionManager;

    IEnumerable<OffsetMapping> IDebugInfo.GetMethodNativeMap(TargetCodePointer pCode, bool preferUninstrumented, out uint codeOffset)
    {
        // Get the method's DebugInfo
        if (_eman.GetCodeBlockHandle(pCode) is not CodeBlockHandle cbh)
            throw new InvalidOperationException($"No CodeBlockHandle found for native code {pCode}.");
        TargetPointer debugInfo = _eman.GetDebugInfo(cbh, out bool hasFlagByte);

        TargetCodePointer nativeCodeStart = _eman.GetStartAddress(cbh);
        codeOffset = (uint)(CodePointerUtils.AddressFromCodePointer(pCode, _target) - CodePointerUtils.AddressFromCodePointer(nativeCodeStart, _target));

        return RestoreBoundaries(debugInfo, hasFlagByte, preferUninstrumented);
    }

    private IEnumerable<OffsetMapping> RestoreBoundaries(TargetPointer debugInfo, bool hasFlagByte, bool preferUninstrumented)
    {
        if (hasFlagByte)
        {
            // Check flag byte and skip over any patchpoint info
            ExtraDebugInfoFlags_1 flagByte = (ExtraDebugInfoFlags_1)_target.Read<byte>(debugInfo++);

            if (flagByte.HasFlag(ExtraDebugInfoFlags_1.EXTRA_DEBUG_INFO_PATCHPOINT))
            {
                Data.PatchpointInfo patchpointInfo = _target.ProcessedData.GetOrAdd<Data.PatchpointInfo>(debugInfo);

                if (_target.GetTypeInfo(DataType.PatchpointInfo).Size is not uint patchpointSize)
                    throw new InvalidOperationException("PatchpointInfo type size is not defined.");
                debugInfo += patchpointSize + (patchpointInfo.LocalCount * sizeof(uint));

                flagByte &= ~ExtraDebugInfoFlags_1.EXTRA_DEBUG_INFO_PATCHPOINT;
            }

            if (flagByte.HasFlag(ExtraDebugInfoFlags_1.EXTRA_DEBUG_INFO_RICH))
            {
                uint richDebugInfoSize = _target.Read<uint>(debugInfo);
                debugInfo += 4;
                debugInfo += richDebugInfoSize;
                flagByte &= ~ExtraDebugInfoFlags_1.EXTRA_DEBUG_INFO_RICH;
            }

            Debug.Assert(flagByte == 0);
        }

        NativeReader nibbleNativeReader = new(new TargetStream(_target, debugInfo, 24 /*maximum size of 4 32bit ints compressed*/), _target.IsLittleEndian);
        NibbleReader nibbleReader = new(nibbleNativeReader, 0);

        uint cbBounds = nibbleReader.ReadUInt();
        uint cbUninstrumentedBounds = 0;
        if (cbBounds == DEBUG_INFO_BOUNDS_HAS_INSTRUMENTED_BOUNDS)
        {
            // This means we have instrumented bounds.
            cbBounds = nibbleReader.ReadUInt();
            cbUninstrumentedBounds = nibbleReader.ReadUInt();
        }
        uint _ /*cbVars*/ = nibbleReader.ReadUInt();

        TargetPointer addrBounds = debugInfo + (uint)nibbleReader.GetNextByteOffset();
        // TargetPointer addrVars = addrBounds + cbBounds + cbUninstrumentedBounds;

        if (preferUninstrumented && cbUninstrumentedBounds != 0)
        {
            // If we have uninstrumented bounds, we will use them instead of the regular bounds.
            addrBounds += cbBounds;
            cbBounds = cbUninstrumentedBounds;
        }

        if (cbBounds > 0)
        {
            NativeReader boundsNativeReader = new(new TargetStream(_target, addrBounds, cbBounds), _target.IsLittleEndian);
            return DebugInfoHelpers.DoBounds(boundsNativeReader, 1);
        }

        return [];
    }
}
