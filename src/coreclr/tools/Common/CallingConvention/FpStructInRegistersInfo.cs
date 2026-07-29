// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// RISC-V and LoongArch64 floating-point struct passing info.
// Extracted from Internal/Runtime/RiscVLoongArch64FpStruct.cs for standalone use.

using System;

namespace Internal.JitInterface
{
    [Flags]
    public enum FpStruct
    {
        PosOnlyOne      = 0,
        PosBothFloat    = 1,
        PosFloatInt     = 2,
        PosIntFloat     = 3,
        PosSizeShift1st = 4,
        PosSizeShift2nd = 6,

        UseIntCallConv = 0,

        OnlyOne          =    1 << PosOnlyOne,
        BothFloat        =    1 << PosBothFloat,
        FloatInt         =    1 << PosFloatInt,
        IntFloat         =    1 << PosIntFloat,
        SizeShift1stMask = 0b11 << PosSizeShift1st,
        SizeShift2ndMask = 0b11 << PosSizeShift2nd,
    }

    public struct FpStructInRegistersInfo
    {
        public FpStruct flags;
        public uint offset1st;
        public uint offset2nd;

        public uint SizeShift1st() { return (uint)((int)flags >> (int)FpStruct.PosSizeShift1st) & 0b11; }
        public uint SizeShift2nd() { return (uint)((int)flags >> (int)FpStruct.PosSizeShift2nd) & 0b11; }

        public uint Size1st() { return 1u << (int)SizeShift1st(); }
        public uint Size2nd() { return 1u << (int)SizeShift2nd(); }
    }
}
