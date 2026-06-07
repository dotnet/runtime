// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class PinnedRvaStaticDoesNotEmitPinStore
{
    private static ReadOnlySpan<byte> Mask => new byte[]
    {
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte ReadFirst()
    {
        fixed (byte* p = &Mask.GetPinnableReference())
        {
            return *p;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte ReadOffset(int index)
    {
        fixed (byte* p = &Mask.GetPinnableReference())
        {
            return *(p + index);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int SumViaTwoFixed()
    {
        int sum;
        fixed (byte* a = &Mask.GetPinnableReference())
        fixed (byte* b = &Mask.GetPinnableReference())
        {
            sum = *a + *(b + 1);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int SumViaSequentialFixed()
    {
        int sum = 0;
        fixed (byte* p = &Mask.GetPinnableReference())
        {
            sum += *p;
        }
        fixed (byte* p = &Mask.GetPinnableReference())
        {
            sum += *(p + 2);
        }
        return sum;
    }

    [Fact]
    public static void ReadFirst_ReturnsZero()
    {
        Assert.Equal(0x00, ReadFirst());
    }

    [Theory]
    [InlineData(0, 0x00)]
    [InlineData(1, 0x01)]
    [InlineData(7, 0x07)]
    [InlineData(15, 0x0F)]
    public static void ReadOffset_ReturnsExpected(int index, byte expected)
    {
        Assert.Equal(expected, ReadOffset(index));
    }

    [Fact]
    public static void SumViaTwoFixed_ReturnsOne()
    {
        Assert.Equal(0x00 + 0x01, SumViaTwoFixed());
    }

    [Fact]
    public static void SumViaSequentialFixed_ReturnsTwo()
    {
        Assert.Equal(0x00 + 0x02, SumViaSequentialFixed());
    }
}
