// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_134538;

public class Runtime_134538
{
    [Fact]
    public static void AluResultsAreZeroExtended()
    {
        Assert.Equal(0UL, AddToUInt64(0, 0));
        Assert.Equal(2_147_483_648UL, AddToUInt64(0x7FFF_FFFFu, 1));
        Assert.Equal(4UL, AddToUInt64(uint.MaxValue, 5));
        Assert.Equal(4L, AddToInt64(uint.MaxValue, 5));

        Assert.Equal(0UL, MultiplyToUInt64(0x8000_0000u, 2));
        Assert.Equal(4_294_967_293UL, MultiplyToUInt64(uint.MaxValue, 3));
        Assert.Equal(4_294_967_293L, MultiplyToInt64(uint.MaxValue, 3));

        Assert.Equal(2_147_483_648UL, ShiftToUInt64(1, 31));
        Assert.Equal(1UL, ShiftToUInt64(1, 32));
        Assert.Equal(2UL, ShiftToUInt64(1, 33));
        Assert.Equal(2_147_483_648UL, ShiftToUInt64(1, -1));
        Assert.Equal(2_147_483_648L, ShiftToInt64(1, -1));

        Assert.Equal(2_147_483_647UL, XorToUInt64(uint.MaxValue, 0x8000_0000u));
        Assert.Equal(4_294_967_295L, XorToInt64(0, uint.MaxValue));

        Assert.Equal(0UL, DivideToUInt64(0, 1));
        Assert.Equal(1_431_655_765UL, DivideToUInt64(uint.MaxValue, 3));
        Assert.Equal(1L, DivideToInt64(uint.MaxValue, uint.MaxValue));
    }

    [Fact]
    public static void AndResultsAreZeroExtended()
    {
        Assert.Equal(0UL, AndToUInt64(uint.MaxValue, 0));
        Assert.Equal(0x8000_0000UL, AndToUInt64(uint.MaxValue, 0x8000_0000u));
        Assert.Equal(0xFFFF_FFFFUL, AndToUInt64(uint.MaxValue, uint.MaxValue));
        Assert.Equal(0xF0F0_0000L, AndToInt64(0xFFFF_0000u, 0xF0F0_F0F0u));
    }

    [Fact]
    public static void ImmediateOperandsAreCovered()
    {
        Assert.Equal(4UL, AddFive(uint.MaxValue));
        Assert.Equal(4_294_967_293UL, MultiplyByThree(uint.MaxValue));
        Assert.Equal(4_294_967_288UL, ShiftByThree(uint.MaxValue));
        Assert.Equal(2_147_483_647UL, XorSignBit(uint.MaxValue));
        Assert.Equal(1_431_655_765UL, DivideByThree(uint.MaxValue));
    }

    [Fact]
    public static void UnsignedDivisionByZeroStillThrows()
    {
        Assert.Throws<DivideByZeroException>(() => DivideToUInt64(1, 0));
        Assert.Throws<DivideByZeroException>(() => DivideToInt64(1, 0));
    }

    [Fact]
    public static void SixtyFourBitProducersAreNotTreatedAsWritersOfWRegisters()
    {
        Assert.Equal(0x1_0000_0005UL, Add64AndKeepUpperBits(0x1_0000_0000UL, 5));
        Assert.Equal(5UL, Add64ThenNarrowAndWiden(0x1_0000_0000UL, 5));

        Assert.Equal(0x3_0000_0003UL, Multiply64AndKeepUpperBits(0x1_0000_0001UL, 3));
        Assert.Equal(3UL, Multiply64ThenNarrowAndWiden(0x1_0000_0001UL, 3));

        Assert.Equal(0x1_0000_00F0UL, And64AndKeepUpperBits(0x1_FFFF_FFFFUL, 0x1_0000_00F0UL));
        Assert.Equal(0xF0UL, And64ThenNarrowAndWiden(0x1_FFFF_FFFFUL, 0x1_0000_00F0UL));
    }

    [Fact]
    public static void SignedIntWideningStillSignExtends()
    {
        Assert.Equal(-1L, SignedAddAndWiden(-1, 0));
        Assert.Equal(-2_147_483_648L, SignedMultiplyAndWiden(int.MinValue, 1));
        Assert.Equal(-2_147_483_648L, SignedAndAndWiden(-1, int.MinValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AddToUInt64(uint left, uint right) => unchecked(left + right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddToInt64(uint left, uint right) => unchecked(left + right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong MultiplyToUInt64(uint left, uint right) => unchecked(left * right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MultiplyToInt64(uint left, uint right) => unchecked(left * right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ShiftToUInt64(uint value, int count) => unchecked(value << count);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ShiftToInt64(uint value, int count) => unchecked(value << count);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong XorToUInt64(uint left, uint right) => left ^ right;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long XorToInt64(uint left, uint right) => left ^ right;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AndToUInt64(uint left, uint right) => left & right;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AndToInt64(uint left, uint right) => left & right;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong And64AndKeepUpperBits(ulong left, ulong right) => left & right;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong And64ThenNarrowAndWiden(ulong left, ulong right) => (uint)(left & right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SignedAndAndWiden(int left, int right) => left & right;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong DivideToUInt64(uint dividend, uint divisor) => dividend / divisor;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long DivideToInt64(uint dividend, uint divisor) => dividend / divisor;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AddFive(uint value) => unchecked(value + 5u);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong MultiplyByThree(uint value) => unchecked(value * 3u);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ShiftByThree(uint value) => unchecked(value << 3);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong XorSignBit(uint value) => value ^ 0x8000_0000u;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong DivideByThree(uint value) => value / 3u;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Add64AndKeepUpperBits(ulong left, ulong right) => unchecked(left + right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Add64ThenNarrowAndWiden(ulong left, ulong right) => unchecked((uint)(left + right));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Multiply64AndKeepUpperBits(ulong left, ulong right) => unchecked(left * right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Multiply64ThenNarrowAndWiden(ulong left, ulong right) => unchecked((uint)(left * right));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SignedAddAndWiden(int left, int right) => unchecked(left + right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SignedMultiplyAndWiden(int left, int right) => unchecked(left * right);
}
