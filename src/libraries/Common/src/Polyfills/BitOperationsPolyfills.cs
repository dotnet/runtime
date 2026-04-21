// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics;

internal static class BitOperations
{
    private static ReadOnlySpan<byte> TrailingZeroCountDeBruijn =>
    [
        00, 01, 28, 02, 29, 14, 24, 03,
        30, 22, 20, 15, 25, 17, 04, 08,
        31, 27, 13, 23, 21, 19, 16, 07,
        26, 12, 18, 06, 11, 05, 10, 09
    ];

    private static ReadOnlySpan<byte> Log2DeBruijn =>
    [
        00, 09, 01, 10, 13, 21, 02, 29,
        11, 14, 16, 18, 22, 25, 03, 30,
        08, 12, 20, 28, 15, 17, 24, 07,
        19, 27, 23, 06, 26, 05, 04, 31
    ];

    /// <summary>
    /// Returns the integer (floor) log of the specified value, base 2.
    /// Note that by convention, input value 0 returns 0 since log(0) is undefined.
    /// </summary>
    public static int Log2(uint value)
    {
        value |= 1;

        // Fill trailing zeros with ones, eg 00010010 becomes 00011111
        value |= value >> 01;
        value |= value >> 02;
        value |= value >> 04;
        value |= value >> 08;
        value |= value >> 16;

        // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
        return Unsafe.AddByteOffset(
            // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_1100_0100_1010_1100_1101_1101u
            ref MemoryMarshal.GetReference(Log2DeBruijn),
            // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
            (nint)((value * 0x07C4ACDDu) >> 27));
    }

    /// <summary>
    /// Returns the population count (number of bits set) of a mask.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(uint value)
    {
        unchecked
        {
            value -= ((value >> 1) & 0x55555555u);
            value = (value & 0x33333333u) + ((value >> 2) & 0x33333333u);
            return (int)((value + (value >> 4) & 0xF0F0F0Fu) * 0x1010101u) >> 24;
        }
    }

    /// <summary>
    /// Returns the population count (number of bits set) of a mask.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(ulong value)
    {
        const ulong Mask01010101 = 0x5555555555555555UL;
        const ulong Mask00110011 = 0x3333333333333333UL;
        const ulong Mask00001111 = 0x0F0F0F0F0F0F0F0FUL;
        const ulong Mask00000001 = 0x0101010101010101UL;
        value -= ((value >> 1) & Mask01010101);
        value = (value & Mask00110011) + ((value >> 2) & Mask00110011);
        return (int)(unchecked(((value + (value >> 4)) & Mask00001111) * Mask00000001) >> 56);
    }

    /// <summary>
    /// Rotates the specified value left by the specified number of bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateLeft(uint value, int offset) => (value << offset) | (value >> (32 - offset));

    /// <summary>
    /// Rotates the specified value left by the specified number of bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateLeft(ulong value, int offset) => (value << offset) | (value >> (64 - offset));

    /// <summary>
    /// Rotates the specified value right by the specified number of bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateRight(uint value, int offset) => (value >> offset) | (value << (32 - offset));

    /// <summary>
    /// Rotates the specified value right by the specified number of bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateRight(ulong value, int offset) => (value >> offset) | (value << (64 - offset));

    /// <summary>
    /// Count the number of trailing zero bits in an integer value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingZeroCount(uint value)
    {
        // Unguarded fallback contract is 0->0, BSF contract is 0->undefined
        if (value == 0)
        {
            return 32;
        }

        // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
        return Unsafe.AddByteOffset(
            // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_0111_1100_1011_0101_0011_0001u
            ref MemoryMarshal.GetReference(TrailingZeroCountDeBruijn),
            // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
            (nint)(int)(((value & (uint)-(int)value) * 0x077CB531u) >> 27)); // Multi-cast mitigates redundant conv.u8
    }
}
