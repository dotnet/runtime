// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_135522
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Packed
    {
        public long Value;
        public byte Padding;
    }

    [Theory]
    [InlineData(1L, 3L)]
    [InlineData(long.MinValue, long.MaxValue)]
    public static void LoadAfterPostIndex(long first, long second)
    {
        Packed[] values = new Packed[4];
        values[0].Value = first;
        values[2].Value = second;
        Assert.Equal(first ^ second, LoadForward(values));
    }

    [Theory]
    [InlineData(1L, 3L)]
    [InlineData(long.MinValue, long.MaxValue)]
    public static void LoadPairAfterPostIndex(long first, long second)
    {
        long[] values = { second, 11, 22, first, 33, 44, 55, 66 };
        Assert.Equal(first ^ second, LoadBackward(values.AsSpan(3)));
    }

    [Theory]
    [InlineData(1L, 3L)]
    [InlineData(long.MinValue, long.MaxValue)]
    public static void StorePairAfterPostIndex(long first, long second)
    {
        long[] values = { second, 11, 22, first, 33, 44, 55, 66 };
        StoreBackward(values.AsSpan(3), second, first);
        Assert.Equal(new long[] { first, 11, 22, second, 33, 44, 55, 66 }, values);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long LoadForward(Span<Packed> values)
    {
        if (values.Length < 4)
        {
            return 0;
        }

        long first = values[0].Value;
        values = values.Slice(1);
        long second = values[1].Value;
        return first ^ second;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long LoadBackward(Span<long> values)
    {
        if (values.Length < 4)
        {
            return 0;
        }

        long first = values[0];
        values = MemoryMarshal.CreateSpan(ref Unsafe.Subtract(ref MemoryMarshal.GetReference(values), 1), values.Length);
        long second = Unsafe.Subtract(ref MemoryMarshal.GetReference(values), 2);
        return first ^ second;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void StoreBackward(Span<long> values, long first, long second)
    {
        if (values.Length < 4)
        {
            return;
        }

        values[0] = first;
        values = MemoryMarshal.CreateSpan(ref Unsafe.Subtract(ref MemoryMarshal.GetReference(values), 1), values.Length);
        Unsafe.Subtract(ref MemoryMarshal.GetReference(values), 2) = second;
    }
}
