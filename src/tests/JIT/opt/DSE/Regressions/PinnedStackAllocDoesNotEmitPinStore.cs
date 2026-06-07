// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class PinnedStackAllocDoesNotEmitPinStore
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte ReadFirst()
    {
        Span<byte> buf = stackalloc byte[16];
        for (int i = 0; i < 16; i++)
        {
            buf[i] = (byte)i;
        }
        fixed (byte* p = buf)
        {
            return *p;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sum()
    {
        Span<byte> buf = stackalloc byte[8];
        for (int i = 0; i < 8; i++)
        {
            buf[i] = (byte)(i + 1);
        }
        int sum = 0;
        fixed (byte* p = buf)
        {
            for (int i = 0; i < 8; i++)
            {
                sum += p[i];
            }
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int SumViaTwoFixed()
    {
        Span<byte> buf = stackalloc byte[8];
        for (int i = 0; i < 8; i++)
        {
            buf[i] = (byte)(i + 1);
        }
        int sum;
        fixed (byte* a = buf)
        fixed (byte* b = buf)
        {
            sum = *a + *(b + 7);
        }
        return sum;
    }

    [Fact]
    public static void ReadFirst_ReturnsZero()
    {
        Assert.Equal(0, ReadFirst());
    }

    [Fact]
    public static void Sum_ReturnsExpected()
    {
        Assert.Equal(1 + 2 + 3 + 4 + 5 + 6 + 7 + 8, Sum());
    }

    [Fact]
    public static void SumViaTwoFixed_ReturnsExpected()
    {
        Assert.Equal(1 + 8, SumViaTwoFixed());
    }
}
