// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class SubtractedLowerBounds
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Unsigned(int length)
    {
        // ARM64: cmp
        // ARM64-NOT: cmp
        // X64: cmp
        // X64-NOT: cmp
        if ((uint)length < 257 || (uint)(length - 257) < 6)
            return 42;
        return length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SignedFirst(int length) =>
        length < 257 || (uint)(length - 257) < 6 ? 42 : length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LargeUnsignedBound(int length) =>
        (uint)length < int.MaxValue || (uint)(length - int.MaxValue) < int.MaxValue ? 42 : length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int UnrepresentableSignedBound(int length) =>
        length < int.MaxValue || (uint)(length - int.MaxValue) < 1 ? 42 : length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SignedSecond(int length) =>
        (uint)length < 257 || unchecked(length - 257) < 6 ? 42 : length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CheckedSubtract(int length) =>
        (uint)length < 257 || (uint)checked(length - 257) < 6 ? 42 : length;

    [Fact]
    public static void TestEntryPoint()
    {
        int[] inputs = { int.MinValue, -3, -2, -1, 0, 256, 257, 262, 263, 264, int.MaxValue };
        foreach (int length in inputs)
        {
            Assert.Equal((ulong)(uint)length >= 263 ? length : 42, Unsigned(length));
            Assert.Equal(length >= 263 ? length : 42, SignedFirst(length));
            Assert.Equal((ulong)(uint)length >= 2UL * int.MaxValue ? length : 42, LargeUnsignedBound(length));
            Assert.Equal(42, UnrepresentableSignedBound(length));
        }

        Assert.Equal(42, SignedSecond(-1));
        Assert.Equal(int.MinValue, SignedSecond(int.MinValue));
        Assert.Equal(263, SignedSecond(263));
        Assert.Equal(-1, CheckedSubtract(-1));
        Assert.Throws<OverflowException>(() => CheckedSubtract(int.MinValue));
    }
}
