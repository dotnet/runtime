// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.CallingConvention;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// CORCOMPILE_GCREFMAP_TOKENS as defined in src/coreclr/inc/corcompile.h.
/// Mirrors the runtime's tokens so this encoder produces a byte-for-byte
/// identical blob to native GCRefMapBuilder (inc/gcrefmap.h).
/// </summary>
internal enum GCRefMapToken : byte
{
    Skip = 0,
    Ref = 1,
    Interior = 2,
    MethodParam = 3,
    TypeParam = 4,
    VASigCookie = 5,
}

/// <summary>
/// Encodes the argument GCRefMap for a method via the existing
/// <see cref="ICallingConvention.EnumerateArguments"/> contract so the
/// result can be compared byte-for-byte against the runtime's
/// ComputeCallRefMap output (frames.cpp). Used by the cdacstress
/// ArgIterator sub-check.
///
/// Phase 3: handles x64/arm64 primitive, object, interior, and
/// param-type / async-continuation arguments. Returns null (caller treats
/// as E_NOTIMPL) for x86 and for any by-value ValueType argument that
/// might contain GC pointers (struct GC walking is a Phase 4 problem).
/// </summary>
internal static class CallingConventionGCRefMapBuilder
{
    private const int MaxBlobLength = 252;

    /// <summary>
    /// Build the GCRefMap blob for <paramref name="methodDesc"/>.
    /// Returns the byte sequence on success, or null if the method uses
    /// a feature this Phase doesn't yet handle.
    /// </summary>
    public static byte[]? TryBuild(Target target, MethodDescHandle methodDesc)
    {
        IRuntimeInfo runtimeInfo = target.Contracts.RuntimeInfo;
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        ICallingConvention cc = target.Contracts.CallingConvention;

        RuntimeInfoArchitecture arch = runtimeInfo.GetTargetArchitecture();
        bool isX86 = arch is RuntimeInfoArchitecture.X86;

        int pointerSize = target.PointerSize;

        // Walk argument locations and stamp tokens into a sparse offset->token map.
        // Mirrors the runtime's FakeGcScanRoots (frames.cpp:1911) which fills a
        // fake TransitionBlock then walks slot positions to emit tokens.
        SortedDictionary<int, GCRefMapToken> tokens = new();
        IEnumerable<ArgumentLocation> args;
        try
        {
            args = cc.EnumerateArguments(methodDesc);
        }
        catch (NotImplementedException)
        {
            return null;
        }

        GenericContextLoc ctxLoc = GenericContextLoc.None;

        foreach (ArgumentLocation arg in args)
        {
            GCRefMapToken token;
            if (arg.IsThis)
            {
                token = arg.IsValueTypeThis ? GCRefMapToken.Interior : GCRefMapToken.Ref;
            }
            else if (arg.IsParamType)
            {
                // Resolve InstArgMethodDesc vs InstArgMethodTable on demand
                // (cheaper than caching when most methods aren't generic).
                if (ctxLoc == GenericContextLoc.None)
                    ctxLoc = SafeGetGenericContextLoc(rts, methodDesc);

                token = ctxLoc switch
                {
                    GenericContextLoc.InstArgMethodDesc => GCRefMapToken.MethodParam,
                    GenericContextLoc.InstArgMethodTable => GCRefMapToken.TypeParam,
                    _ => GCRefMapToken.Skip,
                };
                if (token == GCRefMapToken.Skip)
                    continue;
            }
            else
            {
                switch ((CorElementType)arg.ElementType)
                {
                    case CorElementType.Class:
                    case CorElementType.String:
                    case CorElementType.Object:
                    case CorElementType.Array:
                    case CorElementType.SzArray:
                        token = GCRefMapToken.Ref;
                        break;

                    case CorElementType.Byref:
                        token = GCRefMapToken.Interior;
                        break;

                    case CorElementType.ValueType:
                        if (arg.IsPassedByRef)
                        {
                            token = GCRefMapToken.Interior;
                        }
                        else
                        {
                            bool emitted = false;

                            if (arg.IsByRefLikeStruct)
                            {
                                // ByRefLike value type (Span<T>, ReadOnlySpan<T>,
                                // ByteRef, any ref struct). Mirrors the runtime's
                                // ByRefPointerOffsetsReporter (siginfo.cpp): walk
                                // the type's instance fields and emit INTERIOR
                                // for each ELEMENT_TYPE_BYREF field at its
                                // in-struct offset. ELEMENT_TYPE_PTR / IntPtr /
                                // void* fields are explicitly NOT reported
                                // (so QCallTypeHandle, ObjectHandleOnStack,
                                // StringHandleOnStack contribute nothing).
                                //
                                // For uncached generic instantiations (Span<int>
                                // whose closed MT isn't loaded), the field
                                // layout lives on the open generic (Span<T>).
                                // The byref/ptr distinction is preserved at the
                                // FieldDesc level regardless of which T closes
                                // the type.
                                TypeHandle probe = arg.TypeHandle;
                                if (probe.Address == TargetPointer.Null)
                                    probe = arg.OpenGenericType;
                                if (probe.Address != TargetPointer.Null)
                                {
                                    EmitByRefLikeInterior(rts, probe, arg.Offset, tokens);
                                    if (tokens.Count > MaxBlobLength)
                                        return null;
                                }
                                emitted = true;
                            }

                            if (rts.ContainsGCPointers(arg.TypeHandle))
                            {
                                // By-value struct with embedded GC pointers: emit one
                                // Ref token per pointer slot inside the struct. Mirrors
                                // the runtime's ReportPointersFromValueTypeArg
                                // (siginfo.cpp). The GCDesc series Offset is relative
                                // to a boxed object's start (including the leading MT
                                // pointer); subtract pointerSize to translate to the
                                // unboxed in-frame layout.
                                int structFieldStart = arg.Offset - pointerSize;
                                foreach ((uint seriesOffset, uint seriesSize) in rts.GetGCDescSeries(arg.TypeHandle))
                                {
                                    int seriesBase = structFieldStart + (int)seriesOffset;
                                    for (int subOff = 0; subOff < (int)seriesSize; subOff += pointerSize)
                                    {
                                        tokens[seriesBase + subOff] = GCRefMapToken.Ref;
                                        if (tokens.Count > MaxBlobLength)
                                            return null;
                                    }
                                }
                                emitted = true;
                            }

                            if (!emitted)
                                continue;
                            continue;
                        }
                        break;

                    default:
                        continue;
                }
            }

            tokens[arg.Offset] = token;
        }

        // No GC-significant arguments. On non-x86 the empty blob is just the
        // pending byte flush. On x86 it still carries the WriteStackPop prefix,
        // so emit that first.
        if (tokens.Count == 0)
        {
            if (!isX86)
                return EmptyBlob();
            Encoder enc0 = default;
            enc0.WriteStackPop(cc.GetCbStackPop(methodDesc) / (uint)pointerSize);
            return enc0.Flush();
        }

        // Walk positions 0..maxPos and look up each one's offset in the token
        // map. This is necessary on x86 because pos-order and offset-order
        // diverge there (argument registers occupy the highest offsets but
        // the lowest positions). On non-x86 the mapping is monotonic so we
        // could iterate the offset map directly, but using OffsetFromGCRefMapPos
        // for both keeps the code path uniform.
        TransitionBlock tb = BuildTransitionBlock(runtimeInfo);

        // For x86 we need to know how many slot positions exist (we'd otherwise
        // miss high-pos register slots when the offset map's max is on the
        // stack). Walk every recorded offset and compute its position; for x86
        // OffsetFromGCRefMapPos is bijective so the inverse is well-defined.
        int maxPos = -1;
        foreach (int offset in tokens.Keys)
        {
            int pos = GCRefMapPosFromOffset(tb, offset, isX86, pointerSize);
            if (pos < 0)
                return null;  // alignment / out-of-range -- conservative skip
            if (pos > maxPos) maxPos = pos;
        }

        Encoder enc = default;
        if (isX86)
            enc.WriteStackPop(cc.GetCbStackPop(methodDesc) / (uint)pointerSize);

        for (int pos = 0; pos <= maxPos; pos++)
        {
            int offset = tb.OffsetFromGCRefMapPos(pos);
            if (tokens.TryGetValue(offset, out GCRefMapToken token) && token != GCRefMapToken.Skip)
            {
                enc.WriteToken((uint)pos, (byte)token);
                if (enc.Length > MaxBlobLength)
                    return null;
            }
        }
        return enc.Flush();
    }

    // Inverse of TransitionBlock.OffsetFromGCRefMapPos. On non-x86 the mapping
    // is offset = first + pos*ptr, so pos = (offset - first) / ptr. On x86 the
    // first NumArgumentRegisters positions are argument registers laid out at
    // OffsetOfArgumentRegisters + ARGUMENTREGISTERS_SIZE - (pos+1)*ptr; the
    // remaining positions are stack args at OffsetOfArgs + (pos - n)*ptr.
    // Returns -1 on misalignment.
    private static int GCRefMapPosFromOffset(TransitionBlock tb, int offset, bool isX86, int pointerSize)
    {
        if (!isX86)
        {
            int delta = offset - tb.OffsetOfFirstGCRefMapSlot;
            if (delta < 0 || delta % pointerSize != 0) return -1;
            return delta / pointerSize;
        }

        // x86: arg registers come first in pos order, then stack args.
        int argRegBase = tb.OffsetOfArgumentRegisters;
        int argRegEnd = argRegBase + tb.NumArgumentRegisters * pointerSize;
        if (offset >= argRegBase && offset < argRegEnd)
        {
            int delta = offset - argRegBase;
            if (delta % pointerSize != 0) return -1;
            // Reverse: pos = NumArgumentRegisters - 1 - (delta / ptr)
            return tb.NumArgumentRegisters - 1 - (delta / pointerSize);
        }
        if (offset >= tb.OffsetOfArgs)
        {
            int delta = offset - tb.OffsetOfArgs;
            if (delta % pointerSize != 0) return -1;
            return tb.NumArgumentRegisters + (delta / pointerSize);
        }
        return -1;
    }

    private static GenericContextLoc SafeGetGenericContextLoc(IRuntimeTypeSystem rts, MethodDescHandle md)
    {
        try
        {
            return rts.GetGenericContextLoc(md);
        }
        catch
        {
            return GenericContextLoc.None;
        }
    }

    // Mirror of runtime ByRefPointerOffsetsReporter (siginfo.cpp): walk the
    // instance fields of a ByRefLike value type and emit one INTERIOR token
    // per ELEMENT_TYPE_BYREF field at its offset within the unboxed struct
    // (so absolute offset is baseOffset + fieldOffset). Recurses into nested
    // ByRefLike value-type fields. ELEMENT_TYPE_PTR / IntPtr / void* fields
    // are deliberately skipped to match runtime behavior for QCall-style
    // handle wrappers.
    private static void EmitByRefLikeInterior(
        IRuntimeTypeSystem rts,
        TypeHandle byRefLikeType,
        int baseOffset,
        SortedDictionary<int, GCRefMapToken> tokens)
    {
        // Bound recursion just in case the data is corrupt / cycles in a dump.
        EmitByRefLikeInteriorRecursive(rts, byRefLikeType, baseOffset, tokens, depth: 0);
    }

    private const int MaxByRefLikeRecursionDepth = 16;

    private static void EmitByRefLikeInteriorRecursive(
        IRuntimeTypeSystem rts,
        TypeHandle byRefLikeType,
        int baseOffset,
        SortedDictionary<int, GCRefMapToken> tokens,
        int depth)
    {
        if (depth > MaxByRefLikeRecursionDepth)
            return;
        if (byRefLikeType.Address == TargetPointer.Null)
            return;

        IEnumerable<TargetPointer> fieldDescs;
        try
        {
            fieldDescs = rts.GetFieldDescList(byRefLikeType);
        }
        catch
        {
            return;
        }

        foreach (TargetPointer fdPtr in fieldDescs)
        {
            bool isStatic;
            CorElementType fieldType;
            uint fieldOffset;
            try
            {
                isStatic = rts.IsFieldDescStatic(fdPtr);
                if (isStatic)
                    continue;
                fieldType = rts.GetFieldDescType(fdPtr);
                fieldOffset = rts.GetFieldDescOffset(fdPtr, fieldDef: null);
            }
            catch
            {
                continue;
            }

            int absOffset = baseOffset + (int)fieldOffset;

            if (fieldType == CorElementType.Byref)
            {
                tokens[absOffset] = GCRefMapToken.Interior;
            }
            else if (fieldType == CorElementType.ValueType)
            {
                // Nested value-type field. Recurse only if the field's own
                // MethodTable is ByRefLike (matches runtime Find(FieldDesc*)
                // in ByRefPointerOffsetsReporter).
                TypeHandle nested = rts.GetFieldDescApproxTypeHandle(fdPtr);
                if (nested.Address == TargetPointer.Null)
                    continue;
                bool nestedByRefLike;
                try { nestedByRefLike = rts.IsByRefLike(nested); }
                catch { continue; }
                if (!nestedByRefLike)
                    continue;
                EmitByRefLikeInteriorRecursive(rts, nested, absOffset, tokens, depth + 1);
            }
        }
    }

    private static byte[] EmptyBlob()
    {
        Encoder enc = default;
        return enc.Flush();
    }

    private static TransitionBlock BuildTransitionBlock(IRuntimeInfo runtimeInfo)
    {
        RuntimeInfoArchitecture arch = runtimeInfo.GetTargetArchitecture();
        RuntimeInfoOperatingSystem os = runtimeInfo.GetTargetOperatingSystem();

        Internal.TypeSystem.TargetArchitecture targetArch = arch switch
        {
            RuntimeInfoArchitecture.X86 => Internal.TypeSystem.TargetArchitecture.X86,
            RuntimeInfoArchitecture.X64 => Internal.TypeSystem.TargetArchitecture.X64,
            RuntimeInfoArchitecture.Arm => Internal.TypeSystem.TargetArchitecture.ARM,
            RuntimeInfoArchitecture.Arm64 => Internal.TypeSystem.TargetArchitecture.ARM64,
            RuntimeInfoArchitecture.LoongArch64 => Internal.TypeSystem.TargetArchitecture.LoongArch64,
            RuntimeInfoArchitecture.RiscV64 => Internal.TypeSystem.TargetArchitecture.RiscV64,
            RuntimeInfoArchitecture.Wasm => Internal.TypeSystem.TargetArchitecture.Wasm32,
            _ => throw new NotSupportedException($"Unsupported architecture: {arch}"),
        };

        bool isWindows = os == RuntimeInfoOperatingSystem.Windows;
        bool isApplePlatform = os == RuntimeInfoOperatingSystem.Apple;

        return TransitionBlock.FromTarget(targetArch, isWindows, isApplePlatform, isArmel: false);
    }

    /// <summary>
    /// Bit-stream encoder mirroring native GCRefMapBuilder (inc/gcrefmap.h).
    /// Every encoding rule -- AppendBit's 7-bit chunks with high-bit continuation,
    /// WriteToken's delta encoding, Flush's final byte -- matches byte-for-byte.
    /// </summary>
    private struct Encoder
    {
        private int _pendingByte;
        private int _bits;
        private uint _pos;
        private List<byte> _bytes;

        public int Length => _bytes?.Count ?? 0;

        private void AppendBit(uint bit)
        {
            _bytes ??= new List<byte>(8);
            if (bit != 0)
            {
                while (_bits >= 7)
                {
                    _bytes.Add((byte)(_pendingByte | 0x80));
                    _pendingByte = 0;
                    _bits -= 7;
                }
                _pendingByte |= 1 << _bits;
            }
            _bits++;
        }

        private void AppendTwoBit(uint bits)
        {
            AppendBit(bits & 1);
            AppendBit(bits >> 1);
        }

        private void AppendInt(uint val)
        {
            do
            {
                AppendBit(val & 1);
                AppendBit((val >> 1) & 1);
                AppendBit((val >> 2) & 1);
                val >>= 3;
                AppendBit(val != 0 ? 1u : 0u);
            }
            while (val != 0);
        }

        // x86-only prefix: encode the callee-popped stack-byte count in pointer-size
        // units before any tokens. Mirrors native GCRefMapBuilder::WriteStackPop
        // (inc/gcrefmap.h). Must be called before the first WriteToken.
        public void WriteStackPop(uint stackPop)
        {
            if (stackPop < 3)
            {
                AppendTwoBit(stackPop);
            }
            else
            {
                AppendTwoBit(3);
                AppendInt(stackPop - 3);
            }
        }

        public void WriteToken(uint pos, uint token)
        {
            uint posDelta = pos - _pos;
            _pos = pos + 1;

            if (posDelta != 0)
            {
                if (posDelta < 4)
                {
                    while (posDelta > 0)
                    {
                        AppendTwoBit(0);
                        posDelta--;
                    }
                }
                else
                {
                    AppendTwoBit(3);
                    AppendInt((posDelta - 4) << 1);
                }
            }

            if (token < 3)
            {
                AppendTwoBit(token);
            }
            else
            {
                AppendTwoBit(3);
                AppendInt(((token - 3) << 1) | 1);
            }
        }

        public byte[] Flush()
        {
            _bytes ??= new List<byte>(1);
            if ((_pendingByte & 0x7F) != 0 || _pos == 0)
                _bytes.Add((byte)(_pendingByte & 0x7F));

            return _bytes.ToArray();
        }
    }
}
