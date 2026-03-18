// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARM;

internal static class LookupValues
{
    /// <summary>
    /// This table provides the register mask described by the given C/L/R/Reg bit
    /// combinations in the compact pdata format, along with the number of VFP
    /// registers to save in bits 16-19.
    /// </summary>
    public static ReadOnlySpan<uint> RegisterMaskLookup =>
    [                // C L R Reg
        0x00010,     // 0 0 0 000
        0x00030,     // 0 0 0 001
        0x00070,     // 0 0 0 010
        0x000f0,     // 0 0 0 011
        0x001f0,     // 0 0 0 100
        0x003f0,     // 0 0 0 101
        0x007f0,     // 0 0 0 110
        0x00ff0,     // 0 0 0 111

        0x10000,     // 0 0 1 000
        0x20000,     // 0 0 1 001
        0x30000,     // 0 0 1 010
        0x40000,     // 0 0 1 011
        0x50000,     // 0 0 1 100
        0x60000,     // 0 0 1 101
        0x70000,     // 0 0 1 110
        0x00000,     // 0 0 1 111

        0x04010,     // 0 1 0 000
        0x04030,     // 0 1 0 001
        0x04070,     // 0 1 0 010
        0x040f0,     // 0 1 0 011
        0x041f0,     // 0 1 0 100
        0x043f0,     // 0 1 0 101
        0x047f0,     // 0 1 0 110
        0x04ff0,     // 0 1 0 111

        0x14000,     // 0 1 1 000
        0x24000,     // 0 1 1 001
        0x34000,     // 0 1 1 010
        0x44000,     // 0 1 1 011
        0x54000,     // 0 1 1 100
        0x64000,     // 0 1 1 101
        0x74000,     // 0 1 1 110
        0x04000,     // 0 1 1 111

        0x00810,     // 1 0 0 000
        0x00830,     // 1 0 0 001
        0x00870,     // 1 0 0 010
        0x008f0,     // 1 0 0 011
        0x009f0,     // 1 0 0 100
        0x00bf0,     // 1 0 0 101
        0x00ff0,     // 1 0 0 110
        0x0ffff,     // 1 0 0 111

        0x1ffff,     // 1 0 1 000
        0x2ffff,     // 1 0 1 001
        0x3ffff,     // 1 0 1 010
        0x4ffff,     // 1 0 1 011
        0x5ffff,     // 1 0 1 100
        0x6ffff,     // 1 0 1 101
        0x7ffff,     // 1 0 1 110
        0x0ffff,     // 1 0 1 111

        0x04810,     // 1 1 0 000
        0x04830,     // 1 1 0 001
        0x04870,     // 1 1 0 010
        0x048f0,     // 1 1 0 011
        0x049f0,     // 1 1 0 100
        0x04bf0,     // 1 1 0 101
        0x04ff0,     // 1 1 0 110
        0x0ffff,     // 1 1 0 111

        0x14800,     // 1 1 1 000
        0x24800,     // 1 1 1 001
        0x34800,     // 1 1 1 010
        0x44800,     // 1 1 1 011
        0x54800,     // 1 1 1 100
        0x64800,     // 1 1 1 101
        0x74800,     // 1 1 1 110
        0x04800,     // 1 1 1 111
    ];

    /// <summary>
    /// This table describes the size of each unwind code, in bytes (lower nibble),
    /// along with the size of the corresponding machine code, in halfwords
    /// (upper nibble).
    /// </summary>
    public static ReadOnlySpan<byte> UnwindOpTable =>
    [
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,   0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,   0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,   0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,   0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,   0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,   0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,   0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,   0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,

        0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,   0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,
        0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,   0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,
        0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,   0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,
        0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,   0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,   0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,   0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21,
        0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21,   0x22, 0x22, 0x22, 0x22, 0x12, 0x12, 0x02, 0x22,
        0x01, 0x01, 0x01, 0x01, 0x01, 0x22, 0x22, 0x13,   0x14, 0x23, 0x24, 0x11, 0x21, 0x10, 0x20, 0x00
    ];

    private const ushort NSET_MASK = 0xff00;
    private const ushort ZSET_MASK = 0xf0f0;
    private const ushort CSET_MASK = 0xcccc;
    private const ushort VSET_MASK = 0xaaaa;
    private const ushort NEQUALV_MASK = unchecked((ushort)((NSET_MASK & VSET_MASK) | (~NSET_MASK & ~VSET_MASK)));

    /// <summary>
    /// The ConditionTable is used to look up the state of a condition
    /// based on the CPSR flags N,Z,C,V, which reside in the upper 4
    /// bits. To use this table, take the condition you are interested
    /// in and use it as the index to look up the UINT16 from the table.
    /// Then right-shift that value by the upper 4 bits of the CPSR,
    /// and the low bit will be the result.
    ///
    /// The bits in the CPSR are ordered (MSB to LSB): N,Z,C,V. Taken
    /// together, this is called the CpsrFlags.
    ///
    /// The macros below are defined such that:
    ///
    ///    N = (NSET_MASK >> CpsrFlags) & 1
    ///    Z = (ZSET_MASK >> CpsrFlags) & 1
    ///    C = (CSET_MASK >> CpsrFlags) & 1
    ///    V = (VSET_MASK >> CpsrFlags) & 1
    ///
    /// Also:
    ///
    ///    (N == V) = (NEQUALV_MASK >> CpsrFlags) & 1
    /// </summary>
    public static ReadOnlySpan<ushort> ConditionTable =>
    [
        // EQ: Z
        ZSET_MASK,
        // NE: !Z
        unchecked((ushort)~ZSET_MASK),
        // CS: C
        CSET_MASK,
        // CC: !C
        unchecked((ushort)~CSET_MASK),
        // MI: N
        NSET_MASK,
        // PL: !N
        unchecked((ushort)~NSET_MASK),
        // VS: V
        VSET_MASK,
        // VC: !V
        unchecked((ushort)~VSET_MASK),
        // HI: C & !Z
        CSET_MASK & ~ZSET_MASK,
        // LO: !C | Z
        unchecked((short)~CSET_MASK | ZSET_MASK),
        // GE: N == V
        NEQUALV_MASK,
        // LT: N != V
        unchecked((ushort)~NEQUALV_MASK),
        // GT: (N == V) & !Z
        NEQUALV_MASK & ~ZSET_MASK,
        // LE: (N != V) | Z
        unchecked((ushort)~NEQUALV_MASK | ZSET_MASK),
        // AL: always
        0xffff,
        // NV: never
        0x0000
    ];


    public const ushort OFFSET_NONE = ushort.MaxValue;
    public readonly struct ARM_CONTEXT_OFFSETS(
        ushort alignment,
        ushort totalSize,
        ImmutableArray<ushort> regOffset,
        ImmutableArray<ushort> fpRegOffset,
        ushort spOffset,
        ushort lrOffset,
        ushort pcOffset,
        ushort cpsrOffset,
        ushort fpscrOffset)
    {
        public readonly ushort Alignment = alignment;
        public readonly ushort TotalSize = totalSize;
        public readonly ImmutableArray<ushort> RegOffset = regOffset;
        public readonly ImmutableArray<ushort> FpRegOffset = fpRegOffset;
        public readonly ushort SpOffset = spOffset;
        public readonly ushort LrOffset = lrOffset;
        public readonly ushort PcOffset = pcOffset;
        public readonly ushort CpsrOffset = cpsrOffset;
        public readonly ushort FpscrOffset = fpscrOffset;
    }

    public static readonly ARM_CONTEXT_OFFSETS TrapFrameOffsets =
        new(
            alignment: 8,
            totalSize: 272,
            regOffset: [248, 252, 256, 260, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, 72],
            fpRegOffset: [184, 192, 200, 208, 216, 224, 232, 240, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE,
                OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE,
                OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE],
            spOffset: 64,
            lrOffset: 68,
            pcOffset: 264,
            cpsrOffset: 268,
            fpscrOffset: 176);


    public static readonly ARM_CONTEXT_OFFSETS MachineFrameOffsets =
        new(
            alignment: 8,
            totalSize: 8,
            regOffset: [OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE],
            fpRegOffset: [OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE,
                OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE,
                OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE],
            spOffset: 0,
            lrOffset: OFFSET_NONE,
            pcOffset: 4,
            cpsrOffset: OFFSET_NONE,
            fpscrOffset: OFFSET_NONE);

    public static readonly ARM_CONTEXT_OFFSETS ContextOffsets =
        new(
            alignment: 16,
            totalSize: 416,
            regOffset: [4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52],
            fpRegOffset: [80, 88, 96, 104, 112, 120, 128, 136, 144, 152, 160, 168, 176, 184, 192, 200, 208, 216, 224, 232, 240, 248, 256, 264, 272, 280, 288, 296, 304, 312, 320, 328],
            spOffset: 56,
            lrOffset: 60,
            pcOffset: 64,
            cpsrOffset: 68,
            fpscrOffset: 72);
}
