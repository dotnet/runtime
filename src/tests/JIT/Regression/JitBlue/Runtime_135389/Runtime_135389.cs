// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Xunit;

// A MOVBE load from a stack local into an extended GPR (r16-r31) was encoded
// with the store opcode, overwriting the local instead of loading it.
public class Runtime_135389
{
    [Fact]
    public static void TestEntryPoint()
    {
        int[] a = new int[15];
        for (int i = 0; i < a.Length; i++)
        {
            a[i] = i + 1;
        }

        long[] b = new long[15];
        for (int i = 0; i < b.Length; i++)
        {
            b[i] = i + 1;
        }

        Assert.Equal(0x10000000 + 90, TestInt32(a));
        Assert.Equal(0x1000000000000000L + 90, TestInt64(b));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(ref int v) => v++;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(ref long v) => v++;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TestInt32(int[] a)
    {
        int v = a[14];
        Consume(ref v);
        int x0 = a[0], x1 = a[1], x2 = a[2], x3 = a[3], x4 = a[4], x5 = a[5], x6 = a[6], x7 = a[7];
        int x8 = a[8], x9 = a[9], x10 = a[10], x11 = a[11], x12 = a[12], x13 = a[13];
        int r = BinaryPrimitives.ReverseEndianness(v);
        int s1 = r + x0 + x1 + x2 + x3 + x4 + x5 + x6 + x7 + x8 + x9 + x10 + x11 + x12 + x13;
        int s2 = r ^ x0 ^ x1 ^ x2 ^ x3 ^ x4 ^ x5 ^ x6 ^ x7 ^ x8 ^ x9 ^ x10 ^ x11 ^ x12 ^ x13;
        return r + (s1 - s2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long TestInt64(long[] a)
    {
        long v = a[14];
        Consume(ref v);
        long x0 = a[0], x1 = a[1], x2 = a[2], x3 = a[3], x4 = a[4], x5 = a[5], x6 = a[6], x7 = a[7];
        long x8 = a[8], x9 = a[9], x10 = a[10], x11 = a[11], x12 = a[12], x13 = a[13];
        long r = BinaryPrimitives.ReverseEndianness(v);
        long s1 = r + x0 + x1 + x2 + x3 + x4 + x5 + x6 + x7 + x8 + x9 + x10 + x11 + x12 + x13;
        long s2 = r ^ x0 ^ x1 ^ x2 ^ x3 ^ x4 ^ x5 ^ x6 ^ x7 ^ x8 ^ x9 ^ x10 ^ x11 ^ x12 ^ x13;
        return r + (s1 - s2);
    }
}
