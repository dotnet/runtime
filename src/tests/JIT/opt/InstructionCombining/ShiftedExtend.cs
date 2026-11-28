// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ShiftedExtend
{
    [Fact]
    public static void TestEntryPoint()
    {
        long[] values = { long.MinValue, long.MinValue + 1, -68719476736, -1, 0, 1, 68719476736, long.MaxValue };
        int[] signed = { int.MinValue, -65537, -129, -1, 0, 1, 127, 65535, int.MaxValue };
        uint[] unsigned = { 0, 1, 127, 65535, int.MaxValue, 0x80000000, uint.MaxValue };

        foreach (long value in values)
        {
            foreach (int index in signed)
            {
                long scaled = (long)index * 4;
                Assert.Equal(unchecked(value + scaled), AddSigned(value, index));
                Assert.Equal(unchecked(value + scaled), AddSignedCommuted(value, index));
                Assert.Equal(unchecked(value - scaled), SubSigned(value, index));
                Assert.Equal(value < scaled, LessSigned(value, index));
                Assert.Equal(value != scaled, NotEqualSigned(value, index));
                Assert.Equal(unchecked(value - scaled) == 0, DifferenceIsZero(value, index));
                Assert.Equal(unchecked(value + (long)index * 32), ShiftTooLarge(value, index));
            }

            foreach (uint index in unsigned)
            {
                long scaled = (long)index * 4;
                Assert.Equal(unchecked(value + scaled), AddUnsigned(value, index));
                Assert.Equal(unchecked(value - scaled), SubUnsigned(value, index));
                Assert.Equal(unchecked(scaled - value), ReverseSubUnsigned(value, index));
                Assert.Equal(value < scaled, LessUnsigned(value, index));
                Assert.Equal(scaled < value, SwappedLessUnsigned(value, index));
                Assert.Equal(value == scaled, EqualUnsigned(value, index));
                Assert.Equal(unchecked((ulong)value) < (ulong)scaled, UnsignedComparison(unchecked((ulong)value), index));
                Assert.Equal(unchecked(value + scaled) == 0, SumIsZero(value, index));
                Assert.Equal(unchecked(value + (long)index * 2), ShiftOne(value, index));
                Assert.Equal(unchecked(value - (long)index * 8), ShiftThree(value, index));
                Assert.Equal(unchecked(value + (long)index * 16), ShiftFour(value, index));
            }
        }

        foreach (int value in signed)
        {
            foreach (int index in signed)
            {
                Assert.Equal(unchecked(value + (byte)index * 4), AddByte(value, index));
                Assert.Equal(unchecked(value - (sbyte)index * 4), SubSByte(value, index));
                Assert.Equal(unchecked(value + (ushort)index * 4), AddUShort(value, index));
                Assert.Equal(unchecked(value - (short)index * 4), SubShort(value, index));
                Assert.Equal(value < (byte)index * 4, CompareByte(value, index));
                Assert.Equal(value < (sbyte)index * 4, CompareSByte(value, index));
            }
        }

        Assert.Equal(long.MaxValue, CheckedAdd(long.MaxValue - 4, 1));
        Assert.Throws<OverflowException>(() => CheckedAdd(long.MaxValue, 1));
        Assert.Equal(long.MinValue, CheckedSub(long.MinValue + 4, 1));
        Assert.Throws<OverflowException>(() => CheckedSub(long.MinValue, 1));
        Assert.Equal(1020, CheckedCast(255));
        Assert.Throws<OverflowException>(() => CheckedCast(256));
        Assert.Throws<OverflowException>(() => CheckedCast(-1));

        int changing = 7;
        Assert.Equal(27, BeforeMutation(ref changing));
        Assert.Equal(8, changing);
        changing = 7;
        Assert.Equal(29, BeforeMutationAdd(ref changing));
        Assert.Equal(8, changing);

        int[] elements = { 10, 20, 30, 40 };
        Assert.Equal(30, Slice(elements, 2)[0]);
        Assert.Equal(30, Element(elements, 2));
        Assert.Equal(30, Load(elements, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => Slice(elements, -1));
        Assert.Throws<IndexOutOfRangeException>(() => Load(elements, 4));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddSigned(long value, int index)
    {
        // ARM64-FULL-LINE: add {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, SXTW #2
        return value + ((long)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddSignedCommuted(long value, int index)
    {
        // ARM64-FULL-LINE: add {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, SXTW #2
        return ((long)index << 2) + value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddUnsigned(long value, uint index)
    {
        // ARM64-FULL-LINE: add {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, UXTW #2
        return value + ((long)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SubSigned(long value, int index)
    {
        // ARM64-FULL-LINE: sub {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, SXTW #2
        return value - ((long)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SubUnsigned(long value, uint index)
    {
        // ARM64-FULL-LINE: sub {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, UXTW #2
        return value - ((long)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReverseSubUnsigned(long value, uint index) => ((long)index << 2) - value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessSigned(long value, int index)
    {
        // ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, SXTW #2
        return value < ((long)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessUnsigned(long value, uint index)
    {
        // ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, UXTW #2
        return value < ((long)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SwappedLessUnsigned(long value, uint index)
    {
        // ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, UXTW #2
        return ((long)index << 2) < value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EqualUnsigned(long value, uint index)
    {
        // ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, UXTW #2
        return value == ((long)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool NotEqualSigned(long value, int index) => value != ((long)index << 2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool UnsignedComparison(ulong value, uint index) => value < ((ulong)index << 2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SumIsZero(long value, uint index)
    {
        // ARM64-FULL-LINE: adds {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, UXTW #2
        return value + ((long)index << 2) == 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool DifferenceIsZero(long value, int index)
    {
        // ARM64-FULL-LINE: subs {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, SXTW #2
        return value - ((long)index << 2) == 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ShiftOne(long value, uint index)
    {
        // ARM64-FULL-LINE: add {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, UXTW #1
        return value + ((long)index << 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ShiftThree(long value, uint index)
    {
        // ARM64-FULL-LINE: sub {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, UXTW #3
        return value - ((long)index << 3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ShiftFour(long value, uint index)
    {
        // ARM64-FULL-LINE: add {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, UXTW #4
        return value + ((long)index << 4);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ShiftTooLarge(long value, int index) => value + ((long)index << 5);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AddByte(int value, int index)
    {
        // ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, UXTB #2
        return value + ((byte)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SubSByte(int value, int index)
    {
        // ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, SXTB #2
        return value - ((sbyte)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AddUShort(int value, int index)
    {
        // ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, UXTH #2
        return value + ((ushort)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SubShort(int value, int index)
    {
        // ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, SXTH #2
        return value - ((short)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CompareByte(int value, int index)
    {
        // ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, UXTB #2
        return value < ((byte)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CompareSByte(int value, int index)
    {
        // ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, SXTB #2
        return value < ((sbyte)index << 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CheckedAdd(long value, uint index) => checked(value + ((long)index << 2));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CheckedSub(long value, uint index) => checked(value - ((long)index << 2));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CheckedCast(int index) => checked((byte)index) << 2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long BeforeMutation(ref int index) => ((long)index << 2) - Increment(ref index);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long BeforeMutationAdd(ref int index) => ((long)index << 2) + Increment(ref index);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Increment(ref int index)
    {
        index++;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Span<int> Slice(Span<int> span, int index)
    {
        // ARM64-FULL-LINE: add {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, UXTW #2
        return span.Slice(index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref int Element(Span<int> span, int index) => ref span[index];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Load(Span<int> span, int index) => span[index];
}
