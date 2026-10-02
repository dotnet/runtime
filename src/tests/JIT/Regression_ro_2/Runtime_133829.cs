// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133829
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong LoadByte(ref byte value) => value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong LoadUShort(ref ushort value) => value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong LoadUInt(ref uint value) => value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong Carry(ulong left, ulong right)
    {
        uint carry = unchecked(left + right) < left ? 1u : 0u;
        return carry;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong CompareSigned(long left, long right)
    {
        uint result = left < right ? 1u : 0u;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong Increment(uint value) => unchecked(value + 1);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong Select(bool takeLoad, ref byte value, uint other)
    {
        uint result;
        if (takeLoad)
        {
            result = value;
        }
        else
        {
            result = other;
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static long LoadSigned(ref int value) => value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong CheckedWiden(int value) => checked((ulong)value);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong CompareSignedReversed(long left, long right) => (uint)(left >= right ? 1 : 0);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong CompareUnsignedReversed(ulong left, ulong right) => (uint)(left <= right ? 1 : 0);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong Mask(uint value) => value & 0x7ff;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong NegativeMask(uint value) => value & 0xfffff800;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong Xor(uint value) => value ^ 1;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong NegativeXor(uint value) => value ^ uint.MaxValue;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong TruncateUShort(uint value) => (ushort)value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong Shift31(ulong value) => (uint)(value >> 31);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong Shift32(ulong value) => (uint)(value >> 32);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong Shift63(ulong value) => (uint)(value >> 63);

    [Fact]
    public static int TestEntryPoint()
    {
        foreach (uint value in new uint[] { 0, 1, 0x7f, 0x80, 0xff, 0x7fff, 0x8000, 0xffff, 0x7fffffff, 0x80000000, uint.MaxValue })
        {
            byte b = (byte)value;
            ushort s = (ushort)value;
            uint u = value;
            int i = (int)value;
            if (LoadByte(ref b) != b || LoadUShort(ref s) != s || LoadUInt(ref u) != value ||
                Increment(value) != (ulong)unchecked(value + 1) || LoadSigned(ref i) != i ||
                Select(true, ref b, value) != b || Select(false, ref b, value) != value ||
                Mask(value) != (value & 0x7ff) || NegativeMask(value) != (value & 0xfffff800) ||
                Xor(value) != (value ^ 1) || NegativeXor(value) != (value ^ uint.MaxValue) ||
                TruncateUShort(value) != s)
            {
                return 101;
            }
        }

        foreach (ulong left in new ulong[] { 0, 1, uint.MaxValue, 0x8000000000000000, ulong.MaxValue })
        {
            if (Shift31(left) != ((left >> 31) & uint.MaxValue) ||
                Shift32(left) != (left >> 32) || Shift63(left) != (left >> 63))
            {
                return 105;
            }

            foreach (ulong right in new ulong[] { 0, 1, uint.MaxValue, 0x8000000000000000, ulong.MaxValue })
            {
                ulong expectedCarry = left > ulong.MaxValue - right ? 1UL : 0UL;
                ulong expectedCompare = (long)left < (long)right ? 1UL : 0UL;
                if (Carry(left, right) != expectedCarry || CompareSigned((long)left, (long)right) != expectedCompare ||
                    CompareSignedReversed((long)left, (long)right) != 1 - expectedCompare ||
                    CompareUnsignedReversed(left, right) != (left <= right ? 1UL : 0UL))
                {
                    return 102;
                }
            }
        }

        if (CheckedWiden(int.MaxValue) != int.MaxValue)
        {
            return 103;
        }

        try
        {
            CheckedWiden(-1);
            return 104;
        }
        catch (OverflowException)
        {
        }

        return 100;
    }
}
