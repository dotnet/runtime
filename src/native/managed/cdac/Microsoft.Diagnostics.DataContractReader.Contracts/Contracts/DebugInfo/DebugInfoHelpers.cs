// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.Reflection.ReadyToRun;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Shared bounds and vars decoding helpers for DebugInfo contract versions.
/// V1: 2-bit enumerated source type.
/// V2: 3-bit flags (CallInstruction, StackEmpty, Async).
/// </summary>
internal static class DebugInfoHelpers
{
    /// <summary>
    /// Mirrors ICorDebugInfo::VarLocType from cordebuginfo.h.
    /// Describes how a variable is stored at a particular point in native code.
    /// </summary>
    private enum VarLocType
    {
        VLT_REG,
        VLT_REG_BYREF,
        VLT_REG_FP,
        VLT_STK,
        VLT_STK_BYREF,
        VLT_REG_REG,
        VLT_REG_STK,
        VLT_STK_REG,
        VLT_STK2,
        VLT_FPSTK,
        VLT_FIXED_VA,
        VLT_COUNT,
        VLT_INVALID,
    }

    private const uint IL_OFFSET_BIAS = unchecked((uint)-3);
    private const uint SOURCE_TYPE_BITS_V1 = 2;
    private const uint SOURCE_TYPE_BITS_V2 = 3;

    // ICorDebugInfo::MAX_ILNUM sentinel value used for adjusted encoding of var numbers.
    private const uint MAX_ILNUM = unchecked((uint)-6);

    // ICorDebugInfo::CALL_RETURN_ILNUM. Marks a NativeVarInfo whose location holds the
    // return value of a call site. Such entries use a different on-wire layout (see DoVars).
    private const uint CALL_RETURN_ILNUM = unchecked((uint)-5);

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

    /// <summary>
    /// Decodes variable location info from the debug info vars section and produces
    /// public <see cref="DebugVarInfo"/> entries directly.
    /// Mirrors the native DoNativeVarInfo/TransferReader logic from debuginfostore.cpp.
    /// </summary>
    internal static IEnumerable<DebugVarInfo> DoVars(NativeReader nativeReader, bool isX86)
    {
        NibbleReader reader = new(nativeReader, 0);

        uint varCount = reader.ReadUInt();
        if (varCount == 0)
            yield break;

        for (uint i = 0; i < varCount; i++)
        {
            uint varNumber = reader.ReadUInt() + MAX_ILNUM;
            uint startOffset = reader.ReadUInt();
            uint endOffset;
            uint callReturnValueILOffset = 0;
            if (varNumber == CALL_RETURN_ILNUM)
            {
                callReturnValueILOffset = reader.ReadUInt();
                endOffset = startOffset + 1;
            }
            else
            {
                endOffset = startOffset + reader.ReadUInt();
            }
            VarLocType locType = (VarLocType)reader.ReadUInt();

            if (locType is VarLocType.VLT_INVALID or VarLocType.VLT_COUNT)
                continue;

            yield return locType switch
            {
                VarLocType.VLT_REG => new DebugVarInfo
                {
                    StartOffset = startOffset, EndOffset = endOffset, VarNumber = varNumber, CallReturnValueILOffset = callReturnValueILOffset,
                    Kind = DebugVarLocKind.Register, Register = reader.ReadUInt(),
                },
                VarLocType.VLT_REG_FP => new DebugVarInfo
                {
                    StartOffset = startOffset, EndOffset = endOffset, VarNumber = varNumber, CallReturnValueILOffset = callReturnValueILOffset,
                    Kind = DebugVarLocKind.Register, IsFloatingPoint = true, Register = reader.ReadUInt(),
                },
                VarLocType.VLT_REG_BYREF => new DebugVarInfo
                {
                    StartOffset = startOffset, EndOffset = endOffset, VarNumber = varNumber, CallReturnValueILOffset = callReturnValueILOffset,
                    Kind = DebugVarLocKind.Register, Register = reader.ReadUInt(), IsByRef = true,
                },
                VarLocType.VLT_STK => new DebugVarInfo
                {
                    StartOffset = startOffset, EndOffset = endOffset, VarNumber = varNumber, CallReturnValueILOffset = callReturnValueILOffset,
                    Kind = DebugVarLocKind.Stack, BaseRegister = reader.ReadUInt(), StackOffset = ReadEncodedStackOffset(reader, isX86),
                },
                VarLocType.VLT_STK_BYREF => new DebugVarInfo
                {
                    StartOffset = startOffset, EndOffset = endOffset, VarNumber = varNumber, CallReturnValueILOffset = callReturnValueILOffset,
                    Kind = DebugVarLocKind.Stack, BaseRegister = reader.ReadUInt(), StackOffset = ReadEncodedStackOffset(reader, isX86), IsByRef = true,
                },
                VarLocType.VLT_REG_REG => new DebugVarInfo
                {
                    StartOffset = startOffset, EndOffset = endOffset, VarNumber = varNumber, CallReturnValueILOffset = callReturnValueILOffset,
                    Kind = DebugVarLocKind.RegisterRegister, Register = reader.ReadUInt(), Register2 = reader.ReadUInt(),
                },
                VarLocType.VLT_REG_STK => new DebugVarInfo
                {
                    StartOffset = startOffset, EndOffset = endOffset, VarNumber = varNumber, CallReturnValueILOffset = callReturnValueILOffset,
                    Kind = DebugVarLocKind.RegisterStack, Register = reader.ReadUInt(), BaseRegister2 = reader.ReadUInt(), StackOffset2 = ReadEncodedStackOffset(reader, isX86),
                },
                VarLocType.VLT_STK_REG => new DebugVarInfo
                {
                    StartOffset = startOffset, EndOffset = endOffset, VarNumber = varNumber, CallReturnValueILOffset = callReturnValueILOffset,
                    Kind = DebugVarLocKind.StackRegister, StackOffset = ReadEncodedStackOffset(reader, isX86), BaseRegister = reader.ReadUInt(), Register = reader.ReadUInt(),
                },
                VarLocType.VLT_STK2 => new DebugVarInfo
                {
                    StartOffset = startOffset, EndOffset = endOffset, VarNumber = varNumber, CallReturnValueILOffset = callReturnValueILOffset,
                    Kind = DebugVarLocKind.DoubleStack, BaseRegister = reader.ReadUInt(), StackOffset = ReadEncodedStackOffset(reader, isX86),
                },
                // FPSTK and FIXED_VA: consume stream data to keep reader aligned, but no public
                // DebugVarLocKind exists for these rarely-used location types.
                VarLocType.VLT_FPSTK => ConsumeAndDefault(reader.ReadUInt(), startOffset, endOffset, varNumber, callReturnValueILOffset),
                VarLocType.VLT_FIXED_VA => ConsumeAndDefault(reader.ReadUInt(), startOffset, endOffset, varNumber, callReturnValueILOffset),
                _ => new DebugVarInfo
                {
                    StartOffset = startOffset, EndOffset = endOffset, VarNumber = varNumber, CallReturnValueILOffset = callReturnValueILOffset,
                },
            };
        }
    }

    private static DebugVarInfo ConsumeAndDefault(uint _, uint startOffset, uint endOffset, uint varNumber, uint callReturnValueILOffset) => new()
    {
        StartOffset = startOffset, EndOffset = endOffset, VarNumber = varNumber, CallReturnValueILOffset = callReturnValueILOffset,
    };

    private static int ReadEncodedStackOffset(NibbleReader reader, bool isX86)
    {
        int value = reader.ReadInt();
        // On x86, stack offsets are DWORD-aligned and stored divided by sizeof(DWORD)
        return isX86 ? value * sizeof(int) : value;
    }

    /// <summary>
    /// Decodes the AsyncInfo chunk produced by EECodeInfo::SetAsyncInfo in debuginfostore.cpp.
    /// Layout (NibbleWriter):
    ///   ReadUInt:  NumSuspensionPoints
    ///   ReadUInt:  total var count (informational, unused here)
    ///   for each suspension point:
    ///     ReadInt:  delta from previous DiagnosticNativeOffset (first delta is from 0)
    ///     ReadUInt: NumContinuationVars for this suspension point
    ///   for each var (flat, in suspension-point order):
    ///     ReadUInt: VarNumber - MAX_ILNUM   (sentinel-adjusted to keep negative IL var nums positive)
    ///     ReadUInt: Offset
    /// </summary>
    internal static IReadOnlyList<AsyncSuspensionInfo> DoAsyncInfo(NativeReader nativeReader)
    {
        NibbleReader reader = new(nativeReader, 0);

        uint numSuspensionPoints = reader.ReadUInt();
        // Total var count - not used directly; we read each suspension point's count below.
        _ = reader.ReadUInt();

        if (numSuspensionPoints == 0)
            return Array.Empty<AsyncSuspensionInfo>();

        // Phase 1: read suspension-point headers (offset + per-point var count).
        uint[] nativeOffsets = new uint[numSuspensionPoints];
        uint[] varCounts = new uint[numSuspensionPoints];

        long lastOffset = 0;
        for (uint i = 0; i < numSuspensionPoints; i++)
        {
            lastOffset += reader.ReadInt();
            nativeOffsets[i] = (uint)lastOffset;
            varCounts[i] = reader.ReadUInt();
        }

        // Phase 2: read the flat var list and slice it per suspension point.
        AsyncSuspensionInfo[] result = new AsyncSuspensionInfo[numSuspensionPoints];
        for (uint i = 0; i < numSuspensionPoints; i++)
        {
            uint n = varCounts[i];
            AsyncLocalInfo[] locals = n == 0 ? Array.Empty<AsyncLocalInfo>() : new AsyncLocalInfo[n];
            for (uint v = 0; v < n; v++)
            {
                uint ilVarNumber = reader.ReadUInt() + MAX_ILNUM;
                uint offset = reader.ReadUInt();
                locals[v] = new AsyncLocalInfo { Offset = offset, ILVarNumber = ilVarNumber };
            }
            result[i] = new AsyncSuspensionInfo { NativeOffset = nativeOffsets[i], Locals = locals };
        }
        return result;
    }
}
