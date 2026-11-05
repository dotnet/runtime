// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// A pair of constant tests folds to one test of an interval. The interval has four encodings, and the
// fold takes the one with the cheapest constants; a value's type or non-negativity narrows the interval
// before that choice.
public class RangeFoldEncodings
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ExcludedPair(int x)
    {
        // The excluded values are small, their wrapped complement is not.
        // ARM64-NOT: movn
        // ARM64-NOT: movz
        // X64-NOT: -3
        return x != 5 && x != 6;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SignedSecond(int length)
    {
        // The signed test of the interval starts it at the minimum value, where the constants are small.
        // ARM64-NOT: movz
        // ARM64-NOT: movk
        // X64-NOT: 0x7FFFFFF9
        return (uint)length < 257 || unchecked(length - 257) < 6 ? 42 : length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SignedSecondLength(int[] array)
    {
        // The length is never negative by value number, so the signed test cannot see a wrapped value
        // and the pair is one compare.
        // ARM64-NOT: sub
        // X64-NOT: -257
        // X64-NOT: 0x101
        int length = array.Length;
        return (uint)length < 257 || unchecked(length - 257) < 6 ? 42 : length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ByteLocal(byte[] array)
    {
        // A byte is bounded to [0, 255], so "x != 255 && x > 99" is the one interval [100, 254].
        // ARM64-NOT: ccmp
        // X64-NOT: 255
        byte x = array[0];
        if (x != 255 && x > 99)
            return 1;
        return 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CharLocal(string s)
    {
        // ARM64-NOT: ccmp
        // X64-NOT: 0xFFFF
        char c = s[0];
        if (c != '\uFFFF' && c > '\u007F')
            return 1;
        return 2;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        foreach (byte x in new byte[] { 0, 99, 100, 254, 255 })
        {
            Assert.Equal(x != 255 && x > 99 ? 1 : 2, ByteLocal(new[] { x }));
        }

        foreach (char c in new[] { '\0', '\u007F', '\u0080', '\uFFFE', '\uFFFF' })
        {
            Assert.Equal(c != '\uFFFF' && c > '\u007F' ? 1 : 2, CharLocal(c.ToString()));
        }

        foreach (int x in new[] { int.MinValue, -1, 0, 4, 5, 6, 7, int.MaxValue })
        {
            Assert.Equal(x != 5 && x != 6, ExcludedPair(x));
        }

        foreach (int length in new[] { int.MinValue, int.MinValue + 257, -1, 0, 256, 257, 262, 263, int.MaxValue })
        {
            Assert.Equal((uint)length < 257 || unchecked(length - 257) < 6 ? 42 : length, SignedSecond(length));
        }

        foreach (int length in new[] { 0, 256, 257, 262, 263, 300 })
        {
            Assert.Equal(length < 263 ? 42 : length, SignedSecondLength(new int[length]));
        }
    }
}
