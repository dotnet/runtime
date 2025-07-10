// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ILCompiler.Reflection.ReadyToRun;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class DebugInfo_1(Target target) : IDebugInfo
{
    private const uint IL_OFFSET_BIAS = unchecked((uint)-3);

    internal enum SourceTypes_1 : uint
    {
        SourceTypeInvalid = 0x00, // To indicate that nothing else applies
        SequencePoint = 0x01, // The debugger asked for it.
        StackEmpty = 0x02, // The stack is empty here
        CallSite = 0x04, // This is a call site.
        NativeEndOffsetUnknown = 0x08, // Indicates a epilog endpoint
        CallInstruction = 0x10  // The actual instruction of a call.
    }

    internal readonly struct OffsetMapping_1 : IOffsetMapping
    {
        public uint NativeOffset { get; init; }
        public uint ILOffset { get; init; }
        internal SourceTypes_1 InternalSourceType { get; init; }
        public readonly SourceTypes SourceType
        {
            get
            {
                switch (InternalSourceType)
                {
                    case SourceTypes_1.SourceTypeInvalid:
                        return SourceTypes.SourceTypeInvalid;
                    case SourceTypes_1.SequencePoint:
                        return SourceTypes.SequencePoint;
                    case SourceTypes_1.StackEmpty:
                        return SourceTypes.StackEmpty;
                    case SourceTypes_1.CallSite:
                        return SourceTypes.CallSite;
                    case SourceTypes_1.NativeEndOffsetUnknown:
                        return SourceTypes.NativeEndOffsetUnknown;
                    case SourceTypes_1.CallInstruction:
                        return SourceTypes.CallInstruction;
                    default:
                        Debug.Fail($"Unknown source type: {InternalSourceType}");
                        return SourceTypes.SourceTypeInvalid;
                }
            }
        }
    }

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

    IEnumerable<IOffsetMapping> IDebugInfo.GetMethodNativeMap(TargetCodePointer pCode, out uint codeOffset)
    {
        // Get the method's DebugInfo
        if (_eman.GetCodeBlockHandle(pCode) is not CodeBlockHandle cbh)
            throw new InvalidOperationException($"No CodeBlockHandle found for native code {pCode}.");
        TargetPointer debugInfo = _eman.GetDebugInfo(cbh, out bool hasFlagByte);

        TargetCodePointer nativeCodeStart = _eman.GetStartAddress(cbh);
        codeOffset = (uint)(CodePointerUtils.AddressFromCodePointer(pCode, _target) - CodePointerUtils.AddressFromCodePointer(nativeCodeStart, _target));

        return RestoreBoundaries(debugInfo, hasFlagByte);
    }

    private IEnumerable<IOffsetMapping> RestoreBoundaries(TargetPointer debugInfo, bool hasFlagByte)
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

        NativeReader nibbleNativeReader = new(new TargetStream(_target, debugInfo, 12 /*maximum size of 2 32bit ints compressed*/), _target.IsLittleEndian);
        NibbleReader nibbleReader = new(nibbleNativeReader, 0);

        uint cbBounds = nibbleReader.ReadUInt();
        uint _ /*cbVars*/ = nibbleReader.ReadUInt();

        TargetPointer addrBounds = debugInfo + (uint)nibbleReader.GetNextByteOffset();

        if (cbBounds > 0)
        {
            NativeReader boundsNativeReader = new(new TargetStream(_target, addrBounds, cbBounds), _target.IsLittleEndian);
            NibbleReader boundsReader = new(boundsNativeReader, 0);

            uint countEntries = boundsReader.ReadUInt();
            Debug.Assert(countEntries > 0, "Expected at least one entry in bounds.");

            return DoBounds(boundsReader, countEntries);
        }

        return Enumerable.Empty<IOffsetMapping>();
    }

    private static IEnumerable<IOffsetMapping> DoBounds(NibbleReader reader, uint count)
    {
        uint nativeOffset = 0;
        for (uint i = 0; i < count; i++)
        {
            // native offsets are encoded as a delta from the previous offset
            nativeOffset += reader.ReadUInt();

            // il offsets are encoded with a bias of ICorDebugInfo::MAX_MAPPING_VALUE
            uint ilOffset = unchecked(reader.ReadUInt() + IL_OFFSET_BIAS);

            SourceTypes_1 sourceType = (SourceTypes_1)reader.ReadUInt();

            // TODO(cdac debugInfo): Handle cookie

            yield return new OffsetMapping_1
            {
                NativeOffset = nativeOffset,
                ILOffset = ilOffset,
                InternalSourceType = sourceType
            };
        }
    }
}
