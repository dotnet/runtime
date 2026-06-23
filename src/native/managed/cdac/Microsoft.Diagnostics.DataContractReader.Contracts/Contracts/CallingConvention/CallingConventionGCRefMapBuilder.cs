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
        // x86's GCRefMap position encoding is non-monotonic (argument registers
        // come last in pos-order but first in offset-order, with the stack-pop
        // prefix in the bitstream). Defer to a later phase.
        if (arch is RuntimeInfoArchitecture.X86)
            return null;

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
                        else if (rts.ContainsGCPointers(arg.TypeHandle))
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
                            continue;
                        }
                        else
                        {
                            continue;
                        }
                        break;

                    default:
                        continue;
                }
            }

            tokens[arg.Offset] = token;
        }

        // No GC-significant arguments -> a 1-byte blob (a single empty pending byte).
        // The runtime's GCRefMapBuilder::Flush emits the same.
        if (tokens.Count == 0)
            return EmptyBlob();

        // Determine the highest GCRefMap slot position we need to encode.
        // OffsetFromGCRefMapPos on x64/arm64/etc. is offset = first + pos*pointerSize,
        // so the max pos is (maxOffset - first) / pointerSize. The shared cDAC
        // TransitionBlock helper gives us OffsetOfFirstGCRefMapSlot.
        TransitionBlock tb = BuildTransitionBlock(runtimeInfo);
        int maxOffset = 0;
        foreach (int offset in tokens.Keys)
        {
            if (offset > maxOffset) maxOffset = offset;
        }
        int maxPos = (maxOffset - tb.OffsetOfFirstGCRefMapSlot) / pointerSize;
        if (maxPos < 0)
            return null;  // misalignment -- conservative skip

        Encoder enc = default;
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
